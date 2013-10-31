using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using HangFire.States;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class SchedulePoller : IThreadWrappable, IDisposable
    {
        private readonly ILog _logger = LogManager.GetLogger("SchedulePoller");

        private readonly TimeSpan _pollInterval;
        private readonly IRedisClient _redis;
        private readonly StateMachine _stateMachine;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public SchedulePoller(IRedisClientsManager redisManager, TimeSpan pollInterval)
        {
            _redis = redisManager.GetClient();
            _stateMachine = new StateMachine(_redis);

            _pollInterval = pollInterval;
        }

        public bool EnqueueNextScheduledJob()
        {
            var timestamp = JobHelper.ToTimestamp(DateTime.UtcNow);

            var jobId = _redis
                .GetRangeFromSortedSetByLowestScore(
                    "hangfire:schedule", Double.NegativeInfinity, timestamp, 0, 1)
                .FirstOrDefault();

            if (String.IsNullOrEmpty(jobId))
            {
                return false;
            }

            var enqueuedState = new EnqueuedState("Enqueued by the schedule poller");
            _stateMachine.ChangeState(jobId, enqueuedState, ScheduledState.Name);

            return true;
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
        void IThreadWrappable.Work()
        {
            try
            {
                while (true)
                {
                    var wasEnqueued = false;

                    JobServer.RetryOnException(
                        () =>
                        {
                            wasEnqueued = EnqueueNextScheduledJob();
                        }, _cts.Token.WaitHandle);

                    if (wasEnqueued && !_cts.IsCancellationRequested)
                        continue;

                    if (_cts.Token.WaitHandle.WaitOne(_pollInterval))
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Fatal(
                    "Scheduled jobs will not be added to their queues by this server instance: "
                    + "unexpected exception caught in the SchedulePoller thread.",
                    ex);
            }
        }

        void IThreadWrappable.Dispose(Thread thread)
        {
            _cts.Cancel();
            thread.Join();
        }
    }
}
