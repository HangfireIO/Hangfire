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
using System.Linq;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Storage;

namespace Hangfire.States
{
    public class BackgroundJobStateChanger : IBackgroundJobStateChanger
    {
        private const int MaxStateChangeAttempts = 10;
        private static readonly TimeSpan JobLockTimeout = TimeSpan.FromMinutes(15);

        private readonly IStateMachine _coreStateMachine;
        private readonly IStateMachine _stateMachine;

        public BackgroundJobStateChanger()
            : this(JobFilterProviders.Providers)
        {
        }

        public BackgroundJobStateChanger([NotNull] IJobFilterProvider filterProvider)
            : this(filterProvider, new CoreStateMachine())
        {
        }

        internal BackgroundJobStateChanger([NotNull] IJobFilterProvider filterProvider, [NotNull] IStateMachine stateMachine)
        {
            if (stateMachine == null) throw new ArgumentNullException(nameof(stateMachine));

            _coreStateMachine = stateMachine;
            _stateMachine = new StateMachine(filterProvider, stateMachine);
        }
        
        public IState ChangeState(StateChangeContext context)
        {
            // To ensure that job state will be changed only from one of the
            // specified states, we need to ensure that other users/workers
            // are not able to change the state of the job during the
            // execution of this method. To guarantee this behavior, we are
            // using distributed application locks and rely on fact, that
            // any state transitions will be made only within a such lock.
            using (context.Connection.AcquireDistributedJobLock(context.BackgroundJobId, JobLockTimeout))
            {
                var jobData = GetJobData(context);

                if (jobData == null)
                {
                    // The job does not exist. This may happen, because not
                    // all storage backends support foreign keys.
                    return null;
                }

                if (context.ExpectedStates != null && !context.ExpectedStates.Contains(jobData.State, StringComparer.OrdinalIgnoreCase))
                {
                    return null;
                }
                
                var appliedState = context.NewState;

                try
                {
                    jobData.EnsureLoaded();
                }
                catch (JobLoadException ex)
                {
                    // If the job type could not be loaded, we are unable to
                    // load corresponding filters, unable to process the job
                    // and sometimes unable to change its state (the enqueued
                    // state depends on the type of a job).

                    if (!appliedState.IgnoreJobLoadException)
                    {
                        appliedState = new FailedState(ex.InnerException)
                        {
                            Reason = $"Can not change the state to '{appliedState.Name}': target method was not found."
                        };
                    }
                }

                var backgroundJob = new BackgroundJob(context.BackgroundJobId, jobData.Job, jobData.CreatedAt);
                appliedState = ChangeState(context, backgroundJob, appliedState, jobData.State);

                return appliedState;
            }
        }

        private IState ChangeState(
            StateChangeContext context, BackgroundJob backgroundJob, IState toState, string oldStateName)
        {
            Exception exception = null;

            for (var i = 0; i < MaxStateChangeAttempts; i++)
            {
                try
                {
                    using (var transaction = context.Connection.CreateWriteTransaction())
                    {
                        var applyContext = new ApplyStateContext(
                            context.Storage,
                            context.Connection,
                            transaction,
                            backgroundJob,
                            toState,
                            oldStateName);

                        var appliedState = _stateMachine.ApplyState(applyContext);

                        transaction.Commit();

                        return appliedState;
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }

            var failedState = new FailedState(exception)
            {
                Reason = $"Failed to change state to a '{toState.Name}' one due to an exception after {MaxStateChangeAttempts} retry attempts"
            };

            using (var transaction = context.Connection.CreateWriteTransaction())
            {
                _coreStateMachine.ApplyState(new ApplyStateContext(
                    context.Storage,
                    context.Connection,
                    transaction,
                    backgroundJob,
                    failedState,
                    oldStateName));

                transaction.Commit();
            }

            return failedState;
        }

        private static JobData GetJobData(StateChangeContext context)
        {
            var firstAttempt = true;

            while (true)
            {
                var jobData = context.Connection.GetJobData(context.BackgroundJobId);
                if (!String.IsNullOrEmpty(jobData?.State))
                {
                    return jobData;
                }

                if (context.CancellationToken.IsCancellationRequested ||
                    context.CancellationToken == CancellationToken.None)
                {
                    return null;
                }

                Thread.Sleep(firstAttempt ? 0 : 100);
                firstAttempt = false;
            }
        }
    }
}
