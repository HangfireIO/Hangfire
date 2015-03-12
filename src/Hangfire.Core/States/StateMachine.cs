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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Hangfire.Common;
using Hangfire.Storage;

namespace Hangfire.States
{
    internal class StateMachine : IStateMachine
    {
        private static readonly TimeSpan JobLockTimeout = TimeSpan.FromMinutes(15);
        

        private readonly IStorageConnection _connection;
        private readonly IStateChangeProcess _stateChangeProcess;

        public StateMachine(IStorageConnection connection, IStateChangeProcess stateChangeProcess)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (stateChangeProcess == null) throw new ArgumentNullException("stateChangeProcess");

            _connection = connection;
            _stateChangeProcess = stateChangeProcess;
        }

        public IStateChangeProcess Process { get { return _stateChangeProcess; } }

        public string CreateJob(
            Job job,
            IDictionary<string, string> parameters,
            IState state)
        {
            if (job == null) throw new ArgumentNullException("job");
            if (parameters == null) throw new ArgumentNullException("parameters");
            if (state == null) throw new ArgumentNullException("state");

            var createdAt = DateTime.UtcNow;
            var jobId = _connection.CreateExpiredJob(
                job,
                parameters,
                createdAt,
                TimeSpan.FromHours(1));

            var context = new StateContext(jobId, job, createdAt);
            ChangeState(context, state, null);

            return jobId;
        }

        public bool ChangeState(string jobId, IState toState, string[] fromStates, CancellationToken cancellationToken)
        {
            if (jobId == null) throw new ArgumentNullException("jobId");
            if (toState == null) throw new ArgumentNullException("toState");
            if (fromStates != null && fromStates.Length == 0)
            {
                throw new ArgumentException("From states array should be null or non-empty.", "fromStates");
            }

            // To ensure that job state will be changed only from one of the
            // specified states, we need to ensure that other users/workers
            // are not able to change the state of the job during the
            // execution of this method. To guarantee this behavior, we are
            // using distributed application locks and rely on fact, that
            // any state transitions will be made only within a such lock.
            using (_connection.AcquireDistributedJobLock(jobId, JobLockTimeout))
            {
                var jobData = GetJobData(jobId, cancellationToken);

                if (jobData == null)
                {
                    // The job does not exist. This may happen, because not
                    // all storage backends support foreign keys. 
                    return false;
                }

                if (fromStates != null && !fromStates.Contains(jobData.State, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }

                bool loadSucceeded = true;

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

                    if (!toState.IgnoreJobLoadException)
                    {
                        toState = new FailedState(ex.InnerException)
                        {
                            Reason = String.Format(
                                "Can not change the state of a job to '{0}': target method was not found.",
                                toState.Name)
                        };

                        loadSucceeded = false;
                    }
                }

                var context = new StateContext(jobId, jobData.Job, jobData.CreatedAt);
                var stateChanged = ChangeState(context, toState, jobData.State);

                return loadSucceeded && stateChanged;
            }
        }

        private bool ChangeState(StateContext context, IState toState, string oldStateName)
        {
            try
            {
                var electStateContext = new ElectStateContext(context, _connection, this, toState, oldStateName);
                _stateChangeProcess.ElectState(_connection, electStateContext);

                var applyStateContext = new ApplyStateContext(
                    context,
                    electStateContext.CandidateState,
                    oldStateName,
                    electStateContext.TraversedStates);

                ApplyState(applyStateContext, true);

                // State transition has been succeeded.
                return true;
            }
            catch (Exception ex)
            {
                var failedState = new FailedState(ex)
                {
                    Reason = "An exception occurred during the transition of job's state"
                };

                var applyStateContext = new ApplyStateContext(context, failedState, oldStateName, Enumerable.Empty<IState>());

                // We should not use any state changed filters, because
                // some of the could cause an exception.
                ApplyState(applyStateContext, false);

                // State transition has been failed due to exception.
                return false;
            }
        }

        private void ApplyState(ApplyStateContext context, bool useFilters)
        {
            using (var transaction = _connection.CreateWriteTransaction())
            {
                _stateChangeProcess.ApplyState(transaction, context, useFilters);

                transaction.Commit();
			}
        }

        private JobData GetJobData(string jobId, CancellationToken cancellationToken)
        {
            var firstAttempt = true;

            while (true)
            {
                var jobData = _connection.GetJobData(jobId);
                if (jobData == null)
                {
                    return null;
                }

                if (!String.IsNullOrEmpty(jobData.State))
                {
                    return jobData;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                Thread.Sleep(firstAttempt ? 0 : 100);
                firstAttempt = false;
            }
        }
    }
}
