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
using System.Collections.Generic;
using System.Linq;
using HangFire.Common;
using HangFire.Storage;

namespace HangFire.States
{
    internal class StateMachine : IStateMachine
    {
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
            using (_connection.AcquireJobLock(jobId))
            {
                bool loadSucceeded;

                var jobData = _connection.GetJobData(jobId);

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

                try
                {
                    jobData.EnsureLoaded();
                    loadSucceeded = true;
                }
                catch (JobLoadException ex)
                {
                    // If the job type could not be loaded, we are unable to
                    // load corresponding filters, unable to process the job
                    // and sometimes unable to change its state (the enqueued
                    // state depends on the type of a job).

                    toState = new FailedState(ex)
                    {
                        Reason = String.Format(
                            "Could not change the state of the job '{0}' to the '{1}'. See the inner exception for details.",
                            toState.Name, jobId)
                    };

                    loadSucceeded = false;
                }

                var context = new StateContext(jobId, jobData.Job, jobData.CreatedAt, _connection);
                var stateChanged = _stateChangeProcess.ChangeState(context, toState, jobData.State);

                return loadSucceeded && stateChanged;
            }
        }
    }
}
