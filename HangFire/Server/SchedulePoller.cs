using System;
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

            string jobId = null;

            using (var pipeline = _redis.CreatePipeline())
            {
                // By watching the scheduled tasks key we ensure that only one HangFire server
                // will enqueue the first scheduled job at a time. Otherwise we could we can
                // get the situation, when two or more servers will enqueue the same job multiple
                // times.
                pipeline.QueueCommand(x => x.Watch("hangfire:schedule"));
                pipeline.QueueCommand(
                    x => x.GetRangeFromSortedSetByLowestScore(
                        "hangfire:schedule", Double.NegativeInfinity, timestamp, 0, 1),
                    x => jobId = x.FirstOrDefault());

                pipeline.Flush();
            }

            if (!String.IsNullOrEmpty(jobId))
            {
                return EnqueueScheduledJob(jobId);
            }

            // When schedule contains no entries, we should unwatch it's key.
            _redis.UnWatch();
            return false;
        }

        public bool EnqueueScheduledJob(string jobId)
        {
            // To make atomic remove-enqueue call, we should know the target queue name first.
            var queueName = _redis.GetValueFromHash(String.Format("hangfire:job:{0}", jobId), "ScheduledQueue");

            if (!String.IsNullOrEmpty(queueName))
            {
                // This transaction removes the job from the schedule and enqueues it to it's queue.
                // When another server has already performed such an action with the same job id, this
                // transaction will fail. In this case we should re-run this method again.

                // TODO: check that the job is scheduled
                return JobState.Apply(
                    _redis, new EnqueuedState(jobId, "Enqueued by schedule poller.", queueName),
                    ScheduledState.Name);
            }

            return false;
        }
    }
}
