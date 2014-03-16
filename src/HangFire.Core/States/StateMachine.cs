// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.Common;
using HangFire.Common.Filters;
using HangFire.Common.States;
using HangFire.Storage;

namespace HangFire.States
{
    public class StateMachine
    {
        private readonly IDictionary<string, List<JobStateHandler>> _handlers
            = new Dictionary<string, List<JobStateHandler>>();

        private readonly IStorageConnection _connection;

        private readonly Func<JobMethod, IEnumerable<JobFilter>> _getFiltersThunk
            = JobFilterProviders.Providers.GetFilters;

        public StateMachine(IStorageConnection connection)
            : this(connection, GlobalStateHandlerCollection.GetHandlers(), null)
        {
        }

        internal StateMachine(
            IStorageConnection connection,
            IEnumerable<JobStateHandler> stateHandlers,
            IEnumerable<object> filters)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (stateHandlers == null) throw new ArgumentNullException("stateHandlers");

            _connection = connection;

            var handlers = stateHandlers.Union(connection.Storage.GetStateHandlers());
            foreach (var handler in handlers)
            {
                RegisterHandler(handler);
            }
            
            if (filters != null)
            {
                _getFiltersThunk = jd => filters.Select(f => new JobFilter(f, JobFilterScope.Type, null));
            }
        }
        
        public string CreateInState(
            Job job,
            IDictionary<string, string> parameters,
            State state)
        {
            if (job == null) throw new ArgumentNullException("job");
            if (parameters == null) throw new ArgumentNullException("parameters");
            if (state == null) throw new ArgumentNullException("state");

            var invocationData = new InvocationData
            {
                Type = job.MethodData.Type.AssemblyQualifiedName,
                Method = job.MethodData.MethodInfo.Name,
                ParameterTypes = JobHelper.ToJson(job.MethodData.MethodInfo.GetParameters().Select(x => x.ParameterType)),
            };

            var jobId = _connection.CreateExpiredJob(
                invocationData, 
                job.Arguments.ToArray(),
                parameters,
                TimeSpan.FromHours(1));

            var filterInfo = GetFilters(job.MethodData);
            var context = new StateContext(jobId, job.MethodData);
            var changingContext = new StateChangingContext(context, state, "Created", _connection);

            InvokeStateChangingFilters(changingContext, filterInfo.StateChangingFilters);

            // TODO: check return value
            ApplyState(changingContext, filterInfo.StateChangedFilters);

            return jobId;
        }

        public bool ChangeState(
            string jobId, State state, params string[] allowedCurrentStates)
        {
            if (String.IsNullOrWhiteSpace(jobId)) throw new ArgumentNullException("jobId");
            if (state == null) throw new ArgumentNullException("state");

            using (_connection.AcquireJobLock(jobId))
            {
                var jobData = _connection.GetJobStateAndInvocationData(jobId);

                if (jobData == null)
                {
                    // The job does not exist
                    return false;
                }

                if (allowedCurrentStates.Length > 0 && !allowedCurrentStates.Contains(jobData.State))
                {
                    return false;
                }

                JobMethod jobMethod = null;

                try
                {
                    jobMethod = JobMethod.Deserialize(jobData.InvocationData);
                }
                catch (JobLoadException ex)
                {
                    // If the job type could not be loaded, we are unable to
                    // load corresponding filters, unable to process the job
                    // and sometimes unable to change its state (the enqueued
                    // state depends on the type of a job).

                    state = new FailedState(ex)
                    {
                        Reason = String.Format(
                            "Could not change the state of the job '{0}' to the '{1}'. See the inner exception for details.",
                            state.StateName, jobId)
                    }; 
                }

                var context = new StateContext(jobId, jobMethod);
                var stateChangingContext = 
                    new StateChangingContext(context, state, jobData.State, _connection);

                if (jobMethod == null)
                {
                    return ApplyState(stateChangingContext, Enumerable.Empty<IStateChangedFilter>());
                }

                var filterInfo = GetFilters(jobMethod);
                InvokeStateChangingFilters(stateChangingContext, filterInfo.StateChangingFilters);

                return ApplyState(stateChangingContext, filterInfo.StateChangedFilters);
            }
        }

        private bool ApplyState(StateChangingContext stateChangingContext, IEnumerable<IStateChangedFilter> stateChangedFilters)
        {
            var changedContext = new StateApplyingContext(stateChangingContext);
            return changedContext.ApplyState(_handlers, stateChangedFilters);
        }

        private void InvokeStateChangingFilters(
            StateChangingContext context, IEnumerable<IStateChangingFilter> filters)
        {
            foreach (var filter in filters)
            {
                var oldState = context.CandidateState;
                filter.OnStateChanging(context);

                if (oldState != context.CandidateState)
                {
                    using (var transaction = _connection.CreateWriteTransaction())
                    {
                        transaction.AddJobState(context.JobId, oldState, context.JobMethod);
                        transaction.Commit();
                    }
                }
            }
        }

        private JobFilterInfo GetFilters(JobMethod method)
        {
            return new JobFilterInfo(_getFiltersThunk(method));
        }

        private void RegisterHandler(JobStateHandler handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            if (String.IsNullOrEmpty(handler.StateName)) throw new ArgumentNullException("stateName");

            if (!_handlers.ContainsKey(handler.StateName))
            {
                _handlers.Add(handler.StateName, new List<JobStateHandler>());
            }

            _handlers[handler.StateName].Add(handler);
        }
    }
}
