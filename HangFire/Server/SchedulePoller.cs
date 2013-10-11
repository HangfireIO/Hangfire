using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using HangFire.States;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class SchedulePoller : IDisposable
    {
        private readonly ILog _logger = LogManager.GetLogger("SchedulePoller");
        private readonly Thread _pollerThread;

        private readonly TimeSpan _pollInterval;
        private readonly IRedisClient _redis = RedisFactory.Create();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public SchedulePoller(TimeSpan pollInterval)
        {
            _pollInterval = pollInterval;
            _pollerThread = new Thread(Work) { IsBackground = true, Name = "HangFire.SchedulePoller" };
            _pollerThread.Start();
        }

        public void Dispose()
        {
            _cts.Cancel();

            _pollerThread.Interrupt();
            _pollerThread.Join();
            _redis.Dispose();

            _cts.Dispose();
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
        private void Work()
        {
            try
            {
                while (true)
                {
                    if (!EnqueueScheduledJob(DateTime.UtcNow))
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

        public bool EnqueueScheduledJob(DateTime now)
        {
            var timestamp = JobHelper.ToTimestamp(now);

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

        public void EnqueueScheduledJob(string jobId)
        {
            var jobType = _redis.GetValueFromHash(String.Format("hangfire:job:{0}", jobId), "Type");
            var queueName = JobHelper.TryToGetQueueName(jobType);

            // TODO: move job to the failed queue when queue name is empty
            if (!String.IsNullOrEmpty(queueName))
            {
                JobState.Apply(
                    _redis, new EnqueuedState(jobId, "Enqueued by schedule poller.", queueName),
                    ScheduledState.Name);
            }
        }
    }
}
