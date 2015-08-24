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
    internal class StateMachine : IStateMachine
    {
        private static readonly TimeSpan JobLockTimeout = TimeSpan.FromMinutes(15);
        private readonly IStateChangeProcess _stateChangeProcess;

        public StateMachine([NotNull] IStateChangeProcess stateChangeProcess)
        {
            if (stateChangeProcess == null) throw new ArgumentNullException("stateChangeProcess");
            _stateChangeProcess = stateChangeProcess;
        }
        
        /// <summary>
        /// Attempts to change the state of a job, respecting any applicable job filters and state handlers
        /// <remarks>Also ensures that the job data can be loaded for this job</remarks>
        /// </summary>
        /// <returns><c>Null</c> if a constraint has failed or if the job data could not be loaded, otherwise the final applied state</returns>
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
                            Reason = String.Format(
                                "Can not change the state to '{0}': target method was not found.",
                                appliedState.Name)
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
            var electContext = new ElectStateContext(
                context.Storage, context.Connection, backgroundJob, toState, oldStateName);

            _stateChangeProcess.ElectState(electContext);
            
            using (var transaction = context.Connection.CreateWriteTransaction())
            {
                var applyContext = new ApplyStateContext(transaction, electContext);
                _stateChangeProcess.ApplyState(applyContext);

                transaction.Commit();

                return applyContext.NewState;
            }
        }

        private JobData GetJobData(StateChangeContext context)
        {
            var firstAttempt = true;

            while (true)
            {
                var jobData = context.Connection.GetJobData(context.BackgroundJobId);
                if (jobData == null)
                {
                    return null;
                }

                if (!String.IsNullOrEmpty(jobData.State))
                {
                    return jobData;
                }

                if (context.CancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                Thread.Sleep(firstAttempt ? 0 : 100);
                firstAttempt = false;
            }
        }
    }
}
