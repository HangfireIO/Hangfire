using System;
using System.Threading;
using HangFire.States;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class DequeuedJobsWatcher : IThreadWrappable, IDisposable
    {
        private static readonly TimeSpan CheckedTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan SleepTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(15);

        private readonly string _serverName;

        private readonly IRedisClient _redis = RedisFactory.Create();

        private readonly ILog _logger = LogManager.GetLogger(typeof(DequeuedJobsWatcher));

        public DequeuedJobsWatcher(string serverName)
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
                    String.Format("hangfire:server:{0}:dequeued:{1}:lock", _serverName, queue),
                    TimeSpan.FromMinutes(1)))
                {
                    var jobIds = _redis.GetAllItemsFromList(
                        String.Format("hangfire:server:{0}:dequeued:{1}", _serverName, queue));

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

            if (String.IsNullOrEmpty(fetched) && String.IsNullOrEmpty(@checked))
            {
                _redis.SetEntry(
                    String.Format("hangfire:job:{0}:checked", jobId),
                    JobHelper.ToStringTimestamp(DateTime.UtcNow));
            }
            else
            {
                if (TimedOutByFetchedTime(fetched) || TimedOutByCheckedTime(fetched, @checked))
                {
                    TryToRequeueTheJob(jobId);
                    JobServer.RemoveFromFetchedQueue(_redis, jobId, _serverName, queue);
                }
            }
        }

        private void TryToRequeueTheJob(string jobId)
        {
            var jobType = _redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", jobId),
                "Type");

            var queueName = JobHelper.TryToGetQueueName(jobType);

            if (!String.IsNullOrEmpty(queueName))
            {
                JobState.Apply(
                    _redis,
                    new EnqueuedState(jobId, "Requeued due to time out", queueName),
                    EnqueuedState.Name,
                    ProcessingState.Name);
            }
            else
            {
                JobState.Apply(
                    _redis,
                    new FailedState(
                        jobId,
                        "Failed to re-queue the job.",
                        new InvalidOperationException(String.Format("Could not find type '{0}'.", jobType))),
                    EnqueuedState.Name,
                    ProcessingState.Name);
            }
        }

        private static bool TimedOutByFetchedTime(string fetchedTimestamp)
        {
            return !String.IsNullOrEmpty(fetchedTimestamp) &&
                   (DateTime.UtcNow - JobHelper.FromStringTimestamp(fetchedTimestamp) > JobTimeout);
        }

        private static bool TimedOutByCheckedTime(string fetchedTimestamp, string checkedTimestamp)
        {
            if (!String.IsNullOrEmpty(fetchedTimestamp))
            {
                return false;
            }

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
