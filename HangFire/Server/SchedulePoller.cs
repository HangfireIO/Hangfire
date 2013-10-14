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
        private readonly IRedisClient _redis = RedisFactory.Create();

        public SchedulePoller(TimeSpan pollInterval)
        {
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

            if (!String.IsNullOrEmpty(queue))
            {
                JobState.Apply(
                    _redis, new EnqueuedState(jobId, "Enqueued by schedule poller.", queue),
                    ScheduledState.Name);
            }
            else
            {
                JobState.Apply(
                    _redis,
                    new FailedState(jobId, "Could not enqueue the schedule job.",
                                    new InvalidOperationException(String.Format("Could not find type '{0}'.", jobType))),
                    ScheduledState.Name);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
        void IThreadWrappable.Work()
        {
            try
            {
                while (true)
                {
                    if (!EnqueueNextScheduledJob())
                    {
                        Thread.Sleep(_pollInterval);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ThreadInterruptedException)
            {
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
            thread.Interrupt();
            thread.Join();
        }
    }
}
