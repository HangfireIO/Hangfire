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

        private readonly IRedisClient _redis = RedisFactory.Create();

        private readonly ILog _logger = LogManager.GetLogger(typeof(DequeuedJobsWatcher));

        public void Dispose()
        {
            _redis.Dispose();
        }

        public void FindAndRequeueTimedOutJobs()
        {
            var queues = _redis.GetAllItemsFromSet("hangfire:queues");

            foreach (var queue in queues)
            {
                using (_redis.AcquireLock(
                    String.Format("hangfire:queue:{0}:dequeued:lock", queue),
                    TimeSpan.FromMinutes(1)))
                {
                    var jobIds = _redis.GetAllItemsFromList(
                        String.Format("hangfire:queue:{0}:dequeued", queue));

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
                // If the job does not have these flags set, then it is
                // in the implicit 'Dequeued' state. This state has no 
                // information about the time it was dequeued. So we
                // can not do anything with the job in this state, because
                // there are two options:

                // 1. It is going to move to the implicit 'Fetched' state
                //    in a short time.
                // 2. It will stay in the 'Dequeued' state forever due to
                //    it's processing server is dead.

                // To ensure it's server is dead, we'll move the job to
                // the implicit 'Checked' state with the current timestamp
                // and will not do anything else at this pass of the watcher.
                // If job's state will still be 'Checked' on the later passes
                // and after the CheckedTimeout expired, then the server
                // is dead, and we'll re-queue the job.

                _redis.SetEntry(
                    String.Format("hangfire:job:{0}:checked", jobId),
                    JobHelper.ToStringTimestamp(DateTime.UtcNow));

                // Checkpoint #1-2. The job is in the implicit 'Checked' state.
                // It will be re-queued after the CheckedTimeout will be expired.
            }
            else
            {
                if (TimedOutByFetchedTime(fetched) || TimedOutByCheckedTime(fetched, @checked))
                {
                    TryToRequeueTheJob(jobId);
                    JobFetcher.RemoveFromFetchedQueue(_redis, jobId, queue);
                }
            }
        }

        private void TryToRequeueTheJob(string jobId)
        {
            var jobType = _redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", jobId),
                "Type");

            var queue = JobHelper.TryToGetQueue(jobType);

            var recoverFromStates = new[] { EnqueuedState.Name, ProcessingState.Name };

            if (!String.IsNullOrEmpty(queue))
            {
                JobState.Apply(
                    _redis,
                    new EnqueuedState(jobId, "Requeued due to time out", queue),
                    recoverFromStates);
            }
            else
            {
                JobState.Apply(
                    _redis,
                    new FailedState(
                        jobId,
                        "Failed to re-queue the job.",
                        new InvalidOperationException(String.Format("Could not find type '{0}'.", jobType))),
                    recoverFromStates);
            }
        }

        private static bool TimedOutByFetchedTime(string fetchedTimestamp)
        {
            return !String.IsNullOrEmpty(fetchedTimestamp) &&
                   (DateTime.UtcNow - JobHelper.FromStringTimestamp(fetchedTimestamp) > JobTimeout);
        }

        private static bool TimedOutByCheckedTime(string fetchedTimestamp, string checkedTimestamp)
        {
            // If the job has the 'fetched' flag set, then it is
            // in the implicit 'Fetched' state, and it can not be timed
            // out by the 'checked' flag.
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
