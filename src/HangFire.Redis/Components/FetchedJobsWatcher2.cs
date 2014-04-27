using System;
using System.Threading;
using Common.Logging;
using HangFire.Common;
using HangFire.Server;
using HangFire.States;

namespace HangFire.Redis.Components
{
    internal class FetchedJobsWatcher2 : IServerComponent
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(FetchedJobsWatcher));

        private readonly JobStorage _storage;
        private readonly IStateMachineFactory _stateMachineFactory;
        private readonly FetchedJobsWatcherOptions _options;

        public FetchedJobsWatcher2(JobStorage storage, IStateMachineFactory stateMachineFactory)
            : this(storage, stateMachineFactory, new FetchedJobsWatcherOptions())
        {
        }

        public FetchedJobsWatcher2(
            JobStorage storage, 
            IStateMachineFactory stateMachineFactory,
            FetchedJobsWatcherOptions options)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (stateMachineFactory == null) throw new ArgumentNullException("stateMachineFactory");
            if (options == null) throw new ArgumentNullException("options");

            _storage = storage;
            _stateMachineFactory = stateMachineFactory;
            _options = options;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            using (var connection = (RedisConnection)_storage.GetConnection())
            {
                var queues = connection.Redis.GetAllItemsFromSet(
                    RedisStorage.Prefix + "queues");

                foreach (var queue in queues)
                {
                    ProcessQueue(queue, connection);
                }
            }

            cancellationToken.WaitHandle.WaitOne(_options.SleepTimeout);
        }

        private void ProcessQueue(string queue, RedisConnection connection)
        {
// Allowing only one server at a time to process the timed out
            // jobs from the specified queue.

            Logger.DebugFormat(
                "Acquiring the lock for the fetched list of the '{0}' queue...", queue);

            using (connection.Redis.AcquireLock(
                String.Format(RedisStorage.Prefix + "queue:{0}:dequeued:lock", queue),
                _options.FetchedLockTimeout))
            {
                Logger.DebugFormat(
                    "Looking for timed out jobs in the '{0}' queue...", queue);

                var jobIds = connection.Redis.GetAllItemsFromList(
                    String.Format(RedisStorage.Prefix + "queue:{0}:dequeued", queue));

                var requeued = 0;

                foreach (var jobId in jobIds)
                {
                    if (RequeueJobIfTimedOut(connection, jobId, queue))
                    {
                        requeued++;
                    }
                }

                if (requeued == 0)
                {
                    Logger.DebugFormat("No timed out jobs were found in the '{0}' queue", queue);
                }
                else
                {
                    Logger.InfoFormat(
                        "{0} timed out jobs were found in the '{1}' queue and re-queued.",
                        requeued,
                        queue);
                }
            }
        }

        private bool RequeueJobIfTimedOut(RedisConnection connection, string jobId, string queue)
        {
            var flags = connection.Redis.GetValuesFromHash(
                String.Format(RedisStorage.Prefix + "job:{0}", jobId),
                "Fetched",
                "Checked");

            var fetched = flags[0];
            var @checked = flags[1];

            if (String.IsNullOrEmpty(fetched) && String.IsNullOrEmpty(@checked))
            {
                // If the job does not have these flags set, then it is
                // in the implicit 'Fetched' state. This state has no 
                // information about the time it was fetched. So we
                // can not do anything with the job in this state, because
                // there are two options:

                // 1. It is going to move to the implicit 'Fetched' state
                //    in a short time.
                // 2. It will stay in the 'Fetched' state forever due to
                //    its processing server is dead.

                // To ensure its server is dead, we'll move the job to
                // the implicit 'Checked' state with the current timestamp
                // and will not do anything else at this pass of the watcher.
                // If job's state will still be 'Checked' on the later passes
                // and after the CheckedTimeout expired, then the server
                // is dead, and we'll re-queue the job.

                connection.Redis.SetEntryInHash(
                    String.Format(RedisStorage.Prefix + "job:{0}", jobId),
                    "Checked",
                    JobHelper.ToStringTimestamp(DateTime.UtcNow));

                // Checkpoint #1-2. The job is in the implicit 'Checked' state.
                // It will be re-queued after the CheckedTimeout will be expired.
            }
            else
            {
                if (TimedOutByFetchedTime(fetched) || TimedOutByCheckedTime(fetched, @checked))
                {
                    var stateMachine = _stateMachineFactory.Create(connection);
                    var state = new EnqueuedState
                    {
                        Reason = "Re-queued due to time out"
                    };

                    stateMachine.TryToChangeState(
                        jobId,
                        state,
                        new[] { EnqueuedState.StateName, ProcessingState.StateName });

                    connection.DeleteJobFromQueue(jobId, queue);

                    return true;
                }
            }

            return false;
        }

        private bool TimedOutByFetchedTime(string fetchedTimestamp)
        {
            return !String.IsNullOrEmpty(fetchedTimestamp) &&
                   (DateTime.UtcNow - JobHelper.FromStringTimestamp(fetchedTimestamp) > _options.JobTimeout);
        }

        private bool TimedOutByCheckedTime(string fetchedTimestamp, string checkedTimestamp)
        {
            // If the job has the 'fetched' flag set, then it is
            // in the implicit 'Fetched' state, and it can not be timed
            // out by the 'checked' flag.
            if (!String.IsNullOrEmpty(fetchedTimestamp))
            {
                return false;
            }

            return !String.IsNullOrEmpty(checkedTimestamp) &&
                   (DateTime.UtcNow - JobHelper.FromStringTimestamp(checkedTimestamp) > _options.CheckedTimeout);
        }
    }
}