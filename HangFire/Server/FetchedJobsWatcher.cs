using System;
using System.Threading;
using HangFire.States;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class FetchedJobsWatcher : IThreadWrappable, IDisposable
    {
        private static readonly TimeSpan CheckedTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan SleepTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan JobTimeout = TimeSpan.FromHours(1);

        private readonly string _serverName;

        private readonly IRedisClient _redis = RedisFactory.Create();

        private readonly ILog _logger = LogManager.GetLogger(typeof(FetchedJobsWatcher));

        public FetchedJobsWatcher(string serverName)
        {
            _serverName = serverName;
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        public void FindAndRequeueTimedOutJobs()
        {
            var queues = _redis.GetAllItemsFromSet(
                String.Format("hangfire:server:{0}:queues", _serverName));

            foreach (var queue in queues)
            {
                using (_redis.AcquireLock(
                    String.Format("hangfire:server:{0}:fetched:{1}:lock", _serverName, queue),
                    TimeSpan.FromMinutes(1)))
                {
                    var jobIds = _redis.GetAllItemsFromList(
                        String.Format("hangfire:server:{0}:fetched:{1}", _serverName, queue));

                    foreach (var jobId in jobIds)
                    {
                        RequeueJobIfTimedOut(jobId, queue);
                    }
                }
            }
        }

        private void RequeueJobIfTimedOut(string jobId, string queue)
        {
            string fetched = null;
            string @checked = null;

            using (var pipeline = _redis.CreatePipeline())
            {
                pipeline.QueueCommand(
                    x => x.GetValue(String.Format("hangfire:job:{0}:fetched", jobId)),
                    x => fetched = x);

                pipeline.QueueCommand(
                    x => x.GetValue(String.Format("hangfire:job:{0}:checked", jobId)),
                    x => @checked = x);

                pipeline.Flush();
            }

            // fetched != null -> Fail point >= 2. Check timestamp and requeue if timed out, delete from fetched queue
            // fetched == null, checked == null -> Fail point N1? Fail point == 1. Set checked timestamp and continue.
            // fetched == null, checked != null -> Fail point N1 if timed out. Requeue, delete from fetched queue.

            if (String.IsNullOrEmpty(fetched) && String.IsNullOrEmpty(@checked))
            {
                _redis.SetEntry(
                    String.Format("hangfire:job:{0}:checked", jobId),
                    JobHelper.ToStringTimestamp(DateTime.UtcNow));
            }
            else
            {
                if (TimedOutByFetchedTime(fetched) || TimedOutByCheckedTime(@checked))
                {
                    var jobType = _redis.GetValueFromHash(
                        String.Format("hangfire:job:{0}", jobId),
                        "Type");
                    var queueName = JobHelper.TryToGetQueueName(jobType);

                    // TODO: check the queue name

                    JobState.Apply(
                        _redis,
                        new EnqueuedState(jobId, "Requeued due to time out", queueName),
                        EnqueuedState.Name,
                        ProcessingState.Name);

                    // Fail point.

                    JobServer.RemoveFromFetchedQueue(_redis, jobId, _serverName, queue);
                }
            }
        }

        private static bool TimedOutByFetchedTime(string fetchedTimestamp)
        {
            return !String.IsNullOrEmpty(fetchedTimestamp) &&
                   (DateTime.UtcNow - JobHelper.FromStringTimestamp(fetchedTimestamp) > JobTimeout);
        }

        private static bool TimedOutByCheckedTime(string checkedTimestamp)
        {
            return !String.IsNullOrEmpty(checkedTimestamp) &&
                   (DateTime.UtcNow - JobHelper.FromStringTimestamp(checkedTimestamp) > CheckedTimeout);
        }

        void IThreadWrappable.Work()
        {
            try
            {
                while (true)
                {
                    FindAndRequeueTimedOutJobs();
                    Thread.Sleep(SleepTimeout);
                }
            }
            catch (ThreadInterruptedException)
            {
            }
            catch (Exception ex)
            {
                _logger.Fatal(
                    "Unexpected exception caught in the timed out jobs thread. Timed out jobs will not be re-queued.",
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
