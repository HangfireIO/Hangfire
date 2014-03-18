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
        private readonly StateHandlerCollection _handlers = 
            new StateHandlerCollection();

        private readonly IStorageConnection _connection;

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

            // TODO: enable handlers
            /*var handlers = stateHandlers.Union(connection.Storage.GetStateHandlers());
            foreach (var handler in handlers)
            {
                RegisterHandler(handler);
            }*/

            _getFiltersThunk = md => filters.Select(f => new JobFilter(f, JobFilterScope.Type, null));
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

            // TODO: check return value
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
                    // TODO: transition to the failed state should return false.
                    toState = new FailedState(ex)
                    {
                        Reason = String.Format(
                            "Could not change the state of the job '{0}' to the '{1}'. See the inner exception for details.",
                            toState.Name, jobId)
                    }; 
                }

                var context = new StateContext(jobId, methodData);
                return ChangeState(context, toState, jobData.State);
            }
        }

        internal virtual bool ChangeState(StateContext context, State toState, string oldStateName)
        {
            try
            {
                var changingContext = new StateChangingContext(context, toState, oldStateName, _connection);
                var filterInfo = GetFilters(context.MethodData);

                // TODO: filters may fail
                var electedState = ElectState(changingContext, filterInfo);

                var changedContext = new StateApplyingContext(
                    context,
                    _connection,
                    electedState,
                    oldStateName);

                return changedContext.ApplyState(_handlers, filterInfo.StateChangedFilters);
            }
            catch (Exception ex) // TODO: catch only filter exceptions
            {
                var failedState = new FailedState(ex); // TODO: add reason
                var changedContext = new StateApplyingContext(
                    context,
                    _connection,
                    failedState,
                    oldStateName);
                // TODO: transition to failed state should return false.
                // We should not use any state changed filters, because
                // some of the could cause an exception.
                return changedContext.ApplyState(_handlers, Enumerable.Empty<IStateChangedFilter>());
            }
        }

        internal virtual State ElectState(StateChangingContext context, JobFilterInfo filterInfo)
        {
            var filters = filterInfo.StateChangingFilters;

            foreach (var filter in filters)
            {
                var oldState = context.CandidateState;
                filter.OnStateChanging(context);

                if (oldState != context.CandidateState)
                {
                    using (var transaction = _connection.CreateWriteTransaction())
                    {
                        transaction.AddJobState(context.JobId, oldState);
                        transaction.Commit();
                    }
                }
            }

            return context.CandidateState;
        }

        private JobFilterInfo GetFilters(MethodData methodData)
        {
            return new JobFilterInfo(_getFiltersThunk(methodData));
        }
    }
}
