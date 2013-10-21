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

        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);
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

            EnqueueScheduledJob(jobId);
            return true;
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        private void EnqueueScheduledJob(string jobId)
        {
            var jobType = _redis.GetValueFromHash(String.Format("hangfire:job:{0}", jobId), "Type");
            var queue = JobHelper.TryToGetQueue(jobType);

            JobState state;

            if (!String.IsNullOrEmpty(queue))
            {
                state = new EnqueuedState("Enqueued by schedule poller.", queue);
            }
            else
            {
                state = new FailedState("Could not enqueue the schedule job.",
                    new InvalidOperationException(String.Format("Could not find the type '{0}'.", jobType)));
            }

            _stateMachine.ChangeState(jobId, state, ScheduledState.Name);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
        void IThreadWrappable.Work()
        {
            try
            {
                while (true)
                {
                    var wasEnqueued = EnqueueNextScheduledJob();

                    if (wasEnqueued && !_cts.IsCancellationRequested) 
                        continue;

                    if (_stopped.WaitOne(_pollInterval))
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
            _stopped.Set();
            thread.Join();
        }
    }
}
