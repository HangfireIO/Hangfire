// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;

namespace Hangfire.Server
{
    public class SchedulePoller : IBackgroundProcess
    {
        public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromSeconds(15);

        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromMinutes(1);

        private readonly IStateMachineFactoryFactory _stateMachineFactoryFactory;
        private readonly TimeSpan _pollingInterval;

        private int _enqueuedCount;

        public SchedulePoller() 
            : this(DefaultPollingInterval)
        {
        }

        public SchedulePoller(TimeSpan pollingInterval)
            : this(pollingInterval, new StateMachineFactoryFactory())
        {
        }

        public SchedulePoller(TimeSpan pollingInterval, IStateMachineFactoryFactory stateMachineFactoryFactory)
        {
            if (stateMachineFactoryFactory == null) throw new ArgumentNullException("stateMachineFactoryFactory");

            _stateMachineFactoryFactory = stateMachineFactoryFactory;
            _pollingInterval = pollingInterval;
        }

        public void Execute(BackgroundProcessContext context)
        {
            if (!EnqueueNextScheduledJob(context))
            {
                if (_enqueuedCount != 0)
                {
                    Logger.InfoFormat("{0} scheduled jobs were enqueued.", _enqueuedCount);
                    _enqueuedCount = 0;
                }

                context.Sleep(_pollingInterval);
            }
            else
            {
                // No wait, try to fetch next scheduled job immediately.
                _enqueuedCount++;
            }
        }

        public override string ToString()
        {
            return "Schedule Poller";
        }

        private bool EnqueueNextScheduledJob(BackgroundProcessContext context)
        {
            using (var connection = context.Storage.GetConnection())
            using (connection.AcquireDistributedLock("locks:schedulepoller", DefaultLockTimeout))
            {
                var timestamp = JobHelper.ToTimestamp(DateTime.UtcNow);

                // TODO: it is very slow. Add batching.
                var jobId = connection
                    .GetFirstByLowestScoreFromSet("schedule", 0, timestamp);

                if (String.IsNullOrEmpty(jobId))
                {
                    // No more scheduled jobs pending.
                    return false;
                }

                var stateMachine = _stateMachineFactoryFactory.CreateFactory(context.Storage).Create(connection);
                var enqueuedState = new EnqueuedState
                {
                    Reason = "Triggered scheduled job"
                };

                if (stateMachine.ChangeState(jobId, enqueuedState, new[] { ScheduledState.StateName }) == null)
                {
                    // When a background job with the given id does not exist, we should
                    // remove its id from a schedule manually. This may happen when someone
                    // modifies a storage bypassing Hangfire API.
                    using (var transaction = connection.CreateWriteTransaction())
                    {
                        transaction.RemoveFromSet("schedule", jobId);
                        transaction.Commit();
                    }
                }

                return true;
            }
        }
    }
}