// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading;
using Common.Logging;
using HangFire.Common;
using HangFire.Server;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Redis.Components
{
    internal class FetchedJobsWatcher : IThreadWrappable
    {
        private static readonly TimeSpan FetchedLockTimeout = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan CheckedTimeout = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan SleepTimeout = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(15);

        private static readonly ILog Logger = LogManager.GetLogger(typeof(FetchedJobsWatcher));

        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);

        private readonly RedisStorage _storage;
        private readonly IStateMachineFactory _stateMachineFactory;

        public FetchedJobsWatcher(RedisStorage storage)
        {
            _storage = storage;
            _stateMachineFactory = new StateMachineFactory(storage);
        }

        public void FindAndRequeueTimedOutJobs()
        {
            using (var connection = (RedisConnection)_storage.GetConnection())
            {
                var queues = connection.Redis.GetAllItemsFromSet("hangfire:queues");

                foreach (var queue in queues)
                {
                    // Allowing only one server at a time to process the timed out
                    // jobs from the specified queue.

                    Logger.DebugFormat(
                        "Acquiring the lock for the fetched list of the '{0}' queue...", queue);

                    using (connection.Redis.AcquireLock(
                        String.Format("hangfire:queue:{0}:dequeued:lock", queue),
                        FetchedLockTimeout))
                    {
                        Logger.DebugFormat(
                            "Looking for timed out jobs in the '{0}' queue...", queue);

                        var jobIds = connection.Redis.GetAllItemsFromList(
                            String.Format("hangfire:queue:{0}:dequeued", queue));

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
            }
        }

        private bool RequeueJobIfTimedOut(RedisConnection connection, string jobId, string queue)
        {
            var flags = connection.Redis.GetValuesFromHash(
                String.Format("hangfire:job:{0}", jobId),
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
                    String.Format("hangfire:job:{0}", jobId),
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
                    var state = new EnqueuedState{
                        Reason = "Re-queued due to time out"
                    };

                    stateMachine.TryToChangeState(
                        jobId, 
                        state, 
                        new [] { EnqueuedState.StateName, ProcessingState.StateName });

                    connection.DeleteJobFromQueue(jobId, queue);

                    return true;
                }
            }

            return false;
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
                Logger.Info("Fetched jobs watcher has been started.");

                while (true)
                {
                    JobServer.RetryOnException(FindAndRequeueTimedOutJobs, _stopped);

                    if (_stopped.WaitOne(SleepTimeout))
                    {
                        break;
                    }
                }

                Logger.Info("Fetched jobs watcher has been stopped.");
            }
            catch (Exception ex)
            {
                Logger.Fatal(
                    "Unexpected exception caught in the fetched jobs watcher. Timed out jobs will not be re-queued.",
                    ex);
            }
        }

        void IThreadWrappable.Dispose(Thread thread)
        {
            _stopped.Set();
            thread.Join();
        }
    }
}
