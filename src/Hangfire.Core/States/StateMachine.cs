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

        private readonly JobStorage _storage;
        private readonly IStorageConnection _connection;
        private readonly IStateChangeProcess _stateChangeProcess;

        public StateMachine(
            [NotNull] JobStorage storage,
            [NotNull] IStorageConnection connection, 
            [NotNull] IStateChangeProcess stateChangeProcess)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (connection == null) throw new ArgumentNullException("connection");
            if (stateChangeProcess == null) throw new ArgumentNullException("stateChangeProcess");

            _storage = storage;
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

            var backgroundJob = new BackgroundJob(jobId, job, createdAt);
            ChangeState(backgroundJob, state, null);

            return backgroundJob.Id;
        }

        /// <summary>
        /// Attempts to change the state of a job, respecting any applicable job filters and state handlers
        /// <remarks>Also ensures that the job data can be loaded for this job</remarks>
        /// </summary>
        /// <param name="jobId">The ID of the job to be changed</param>
        /// <param name="toState">The new state to change to</param>
        /// <param name="fromStates">Constraints for the initial job state to change from, optional</param>
        /// <param name="cancellationToken">A cancellation token used while loading job data</param>
        /// <returns><c>Null</c> if a constraint has failed or if the job data could not be loaded, otherwise the final applied state</returns>
        public IState ChangeState(string jobId, IState toState, string[] fromStates, CancellationToken cancellationToken)
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
                    return null;
                }

                if (fromStates != null && !fromStates.Contains(jobData.State, StringComparer.OrdinalIgnoreCase))
                {
                    return null;
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
                                "Can not change the state to '{0}': target method was not found.",
                                toState.Name)
                        };

                        loadSucceeded = false;
                    }
                }

                var backgroundJob = new BackgroundJob(jobId, jobData.Job, jobData.CreatedAt);
                var appliedState = ChangeState(backgroundJob, toState, jobData.State);

                // Only return the applied state if everything loaded correctly
                return loadSucceeded ? appliedState : null;
            }
        }

        private IState ChangeState(BackgroundJob backgroundJob, IState toState, string oldStateName)
        {
            var electContext = new ElectStateContext(
                _storage, _connection, backgroundJob, toState, oldStateName);

            _stateChangeProcess.ElectState(electContext);
            
            using (var transaction = _connection.CreateWriteTransaction())
            {
                var applyContext = new ApplyStateContext(transaction, electContext);
                _stateChangeProcess.ApplyState(applyContext);

                transaction.Commit();

                return applyContext.NewState;
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
