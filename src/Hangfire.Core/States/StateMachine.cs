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
using Hangfire.Common;
using Hangfire.Storage;

namespace Hangfire.States
{
    internal class StateMachine : IStateMachine
    {
        private static readonly TimeSpan JobLockTimeout = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan JobFetchTimeout = TimeSpan.FromSeconds(15);

        private readonly IStorageConnection _connection;
        private readonly IStateChangeProcess _stateChangeProcess;

        public StateMachine(IStorageConnection connection, IStateChangeProcess stateChangeProcess)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (stateChangeProcess == null) throw new ArgumentNullException("stateChangeProcess");

            _connection = connection;
            _stateChangeProcess = stateChangeProcess;
        }

        public string CreateInState(
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

            var context = new StateContext(jobId, job, createdAt, _connection);
            _stateChangeProcess.ChangeState(context, state, null);

            return jobId;
        }

        public bool TryToChangeState(
            string jobId, IState toState, string[] fromStates)
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
            using (_connection.AcquireDistributedLock(
                String.Format("job:{0}:state-lock", jobId),
                JobLockTimeout))
            {
                var jobData = GetJobData(jobId, JobFetchTimeout);

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

                var context = new StateContext(jobId, jobData.Job, jobData.CreatedAt, _connection);
                var stateChanged = _stateChangeProcess.ChangeState(context, toState, jobData.State);

                return loadSucceeded && stateChanged;
            }
        }

        private JobData GetJobData(string jobId, TimeSpan timeout)
        {
            var started = DateTime.UtcNow;
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

                if (DateTime.UtcNow >= started.Add(timeout))
                {
                    throw new TimeoutException(String.Format(
                        "Timeout expired while trying to fetch a non-null state for background job '{0}'.",
                        jobId));
                }

                Thread.Sleep(firstAttempt ? 0 : 100);
                firstAttempt = false;
            }
        }
    }
}
