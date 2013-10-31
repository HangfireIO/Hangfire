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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SchedulePoller));

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
                    "hangfire:schedule", 0, timestamp, 0, 1)
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
                Logger.Info("Schedule poller has been started.");

                int enqueued = 0;

                while (true)
                {
                    var wasEnqueued = false;

                    JobServer.RetryOnException(
                        () =>
                        {
                            wasEnqueued = EnqueueNextScheduledJob();
                        }, _cts.Token.WaitHandle);

                    if (wasEnqueued && !_cts.IsCancellationRequested)
                    {
                        enqueued++;
                        continue;
                    }

                    if (enqueued != 0)
                    {
                        Logger.InfoFormat("{0} scheduled jobs were enqueued.", enqueued);
                        enqueued = 0;
                    }

                    if (_cts.Token.WaitHandle.WaitOne(_pollInterval))
                    {
                        break;
                    }
                }

                Logger.Info("Schedule poller has been stopped.");
            }
            catch (Exception ex)
            {
                Logger.Fatal(
                    "Unexpected exception caught in the schedule poller. Scheduled jobs will not be added to their queues.",
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
