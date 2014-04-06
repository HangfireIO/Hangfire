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
using HangFire.Common.Filters;
using HangFire.Common.States;
using HangFire.Storage;

namespace HangFire.States
{
    public class StateMachine : IStateMachine
    {
        private readonly IStorageConnection _connection;
        private StateHandlerCollection _handlersCache;

        private readonly Func<IStorageConnection, IEnumerable<StateHandler>> _getHandlersThunk
            = GetStateHandlers;
        private readonly Func<MethodData, IEnumerable<JobFilter>> _getFiltersThunk
            = JobFilterProviders.Providers.GetFilters;

        public StateMachine(IStorageConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            _connection = connection;
        }

        internal StateMachine(
            IStorageConnection connection,
            IEnumerable<StateHandler> handlers,
            IEnumerable<object> filters)
            : this(connection)
        {
            if (handlers == null) throw new ArgumentNullException("handlers");
            if (filters == null) throw new ArgumentNullException("filters");

            _getFiltersThunk = md => filters.Select(f => new JobFilter(f, JobFilterScope.Type, null));
            _getHandlersThunk = c => handlers;
        }

        public string CreateInState(
            Job job,
            IDictionary<string, string> parameters,
            State state)
        {
            if (job == null) throw new ArgumentNullException("job");
            if (parameters == null) throw new ArgumentNullException("parameters");
            if (state == null) throw new ArgumentNullException("state");

            var invocationData = job.MethodData.Serialize();

            var jobId = _connection.CreateExpiredJob(
                invocationData, 
                job.Arguments.ToArray(),
                parameters,
                TimeSpan.FromHours(1));

            var context = new StateContext(jobId, job.MethodData);
            ChangeState(context, state, null);

            return jobId;
        }

        public bool TryToChangeState(
            string jobId, State toState, string[] fromStates)
        {
            if (jobId == null) throw new ArgumentNullException("jobId");
            if (toState == null) throw new ArgumentNullException("toState");
            if (fromStates == null) throw new ArgumentNullException("fromStates");

            // To ensure that job state will be changed only from one of the
            // specified states, we need to ensure that other users/workers
            // are not able to change the state of the job during the
            // execution of this method. To guarantee this behavior, we are
            // using distributed application locks and rely on fact, that
            // any state transitions will be made only within a such lock.
            using (_connection.AcquireJobLock(jobId))
            {
                var jobData = _connection.GetJobStateAndInvocationData(jobId);

                if (jobData == null)
                {
                    // The job does not exist. This may happen, because not
                    // all storage backends support foreign keys. 
                    return false;
                }

                if (!fromStates.Contains(jobData.State, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }

                MethodData methodData = null;
                bool loadSucceeded = true;

                try
                {
                    methodData = MethodData.Deserialize(jobData.InvocationData);
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

                var context = new StateContext(jobId, methodData);
                var stateChanged = ChangeState(context, toState, jobData.State);

                return loadSucceeded && stateChanged;
            }
        }

        internal virtual bool ChangeState(StateContext context, State toState, string oldStateName)
        {
            try
            {
                var filterInfo = GetFilters(context.MethodData);

                var electedState = ElectState(context, toState, oldStateName, filterInfo.ElectStateFilters);
                ApplyState(context, electedState, oldStateName, filterInfo.ApplyStateFilters);

                // State transition was succeeded.
                return true;
            }
            catch (Exception ex)
            {
                var failedState = new FailedState(ex)
                {
                    Reason = "An exception occurred during the transition of job's state"
                };

                // We should not use any state changed filters, because
                // some of the could cause an exception.
                ApplyState(context, failedState, oldStateName, Enumerable.Empty<IApplyStateFilter>());

                // State transition was failed due to exception.
                return false;
            }
        }

        internal virtual State ElectState(
            StateContext stateContext, 
            State toState,
            string fromStateName,
            IEnumerable<IElectStateFilter> filters)
        {
            var context = new ElectStateContext(
                stateContext, toState, fromStateName, _connection);

            return context.ElectState(filters);
        }

        internal virtual void ApplyState(
            StateContext stateContext, 
            State electedState,
            string oldStateName,
            IEnumerable<IApplyStateFilter> filters)
        {
            var context = new ApplyStateContext(
                _connection, stateContext, electedState, oldStateName);

            context.ApplyState(GetHandlers(), filters);
        }

        private JobFilterInfo GetFilters(MethodData methodData)
        {
            return new JobFilterInfo(_getFiltersThunk(methodData));
        }

        private StateHandlerCollection GetHandlers()
        {
            if (_handlersCache == null)
            {
                _handlersCache = new StateHandlerCollection();

                var handlers = _getHandlersThunk(_connection);
                foreach (var handler in handlers)
                {
                    _handlersCache.AddHandler(handler);
                }
            }

            return _handlersCache;
        }

        private static IEnumerable<StateHandler> GetStateHandlers(
            IStorageConnection connection)
        {
            var handlers = new List<StateHandler>();
            handlers.AddRange(GlobalStateHandlers.Handlers);
            handlers.AddRange(connection.Storage.GetStateHandlers());

            return handlers;
        }
    }
}
