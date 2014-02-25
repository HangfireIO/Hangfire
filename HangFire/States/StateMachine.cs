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
        internal static readonly IDictionary<string, JobStateDescriptor> Descriptors
            = new Dictionary<string, JobStateDescriptor>();

        static StateMachine()
        {
            RegisterStateDescriptor(FailedState.Name, new FailedState.Descriptor());
            RegisterStateDescriptor(ProcessingState.Name, new ProcessingState.Descriptor());
            RegisterStateDescriptor(ScheduledState.Name, new ScheduledState.Descriptor());
            RegisterStateDescriptor(SucceededState.Name, new SucceededState.Descriptor());
        }

        public static void RegisterStateDescriptor(
            string stateName, JobStateDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException("descriptor");
            if (String.IsNullOrEmpty(stateName)) throw new ArgumentNullException("stateName");
            
            Descriptors.Add(stateName, descriptor);
        }

        private readonly IStorageConnection _connection;
        private readonly IDictionary<string, JobStateDescriptor> _stateDescriptors;

        private readonly Func<JobMethod, IEnumerable<JobFilter>> _getFiltersThunk
            = JobFilterProviders.Providers.GetFilters;

        public StateMachine(IStorageConnection connection)
            : this(
                connection,
                Descriptors,
                null)
        {
        }

        internal StateMachine(
            IStorageConnection connection,
            IDictionary<string, JobStateDescriptor> stateDescriptors,
            IEnumerable<object> filters)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (stateDescriptors == null) throw new ArgumentNullException("stateDescriptors");

            _connection = connection;
            _stateDescriptors = stateDescriptors;
            
            if (filters != null)
            {
                _getFiltersThunk = jd => filters.Select(f => new JobFilter(f, JobFilterScope.Type, null));
            }
        }
        
        public bool CreateInState(
            string jobId,
            JobMethod method,
            IDictionary<string, string> parameters,
            JobState state)
        {
            if (jobId == null) throw new ArgumentNullException("jobId");
            if (method == null) throw new ArgumentNullException("method");
            if (parameters == null) throw new ArgumentNullException("parameters");
            if (state == null) throw new ArgumentNullException("state");

            using (var transaction = _connection.CreateWriteTransaction())
            {
                transaction.Jobs.Create(jobId, parameters);
                transaction.Jobs.Expire(jobId, TimeSpan.FromHours(1));

                transaction.Commit();
            }

            var filterInfo = GetFilters(method);
            var context = new StateContext(jobId, method);
            var changingContext = new StateChangingContext(context, state, null, _connection);

            InvokeStateChangingFilters(changingContext, filterInfo.StateChangingFilters);

            using (var transaction = _connection.CreateWriteTransaction())
            {
                var changedContext = new StateApplyingContext(
                    context,
                    transaction);

                ApplyState(
                    changedContext, 
                    null,
                    changingContext.CandidateState,
                    filterInfo.StateChangedFilters);

                transaction.Jobs.Persist(jobId);

                return transaction.Commit();
            }
        }

        public bool ChangeState(
            string jobId, JobState state, params string[] allowedCurrentStates)
        {
            if (String.IsNullOrWhiteSpace(jobId)) throw new ArgumentNullException("jobId");
            if (state == null) throw new ArgumentNullException("state");

            using (_connection.AcquireJobLock(jobId))
            {
                var job = _connection.Jobs.Get(jobId);

                if (job == null)
                {
                    // The job does not exist
                    return false;
                }

                var currentState = job["State"];
                if (allowedCurrentStates.Length > 0 && !allowedCurrentStates.Contains(currentState))
                {
                        return false;
                }

                try
                {
                    var jobMethod = JobMethod.Deserialize(job);
                    var filterInfo = GetFilters(jobMethod);

                    var context = new StateContext(jobId, jobMethod);
                    var changingContext = new StateChangingContext(context, state, currentState, _connection);

                    InvokeStateChangingFilters(changingContext, filterInfo.StateChangingFilters);
                    
                    return ApplyState(changingContext, filterInfo.StateChangedFilters);
                }
                catch (JobLoadException ex)
                {
                    // If the job type could not be loaded, we are unable to
                    // load corresponding filters, unable to process the job
                    // and sometimes unable to change its state (the enqueued
                    // state depends on the type of a job).

                    var changingContext = new StateChangingContext(
                        new StateContext(jobId, null),
                        new FailedState(
                            String.Format(
                                "Could not change the state of the job '{0}' to the '{1}'. See the inner exception for details.",
                                state.StateName, jobId),
                            ex),
                        currentState,
                        _connection);

                    return ApplyState(
                        changingContext,
                        Enumerable.Empty<IStateChangedFilter>());
                }
            }
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
                    AppendHistory(context, oldState, false);
                }
            }
        }

        private bool ApplyState(
            StateChangingContext context,
            IEnumerable<IStateChangedFilter> stateChangedFilters)
        {
            using (var transaction = _connection.CreateWriteTransaction())
            {
                var changedContext = new StateApplyingContext(
                    context,
                    transaction);

                ApplyState(changedContext, context.CurrentState, context.CandidateState, stateChangedFilters);

                return transaction.Commit();
            }
        }

        private void ApplyState(
            StateApplyingContext context,
            string oldState,
            JobState chosenState,
            IEnumerable<IStateChangedFilter> stateChangedFilters)
        {
            if (!String.IsNullOrEmpty(oldState))
            {
                if (_stateDescriptors.ContainsKey(oldState))
                {
                    _stateDescriptors[oldState].Unapply(context);
                }

                foreach (var filter in stateChangedFilters)
                {
                    filter.OnStateUnapplied(context);
                }
            }

            AppendHistory(context.Transaction, context, chosenState, true);

            chosenState.Apply(context);

            foreach (var filter in stateChangedFilters)
            {
                filter.OnStateApplied(context);
            }
        }

        private void AppendHistory(
            StateContext context, JobState state, bool appendToJob)
        {
            using (var transaction = _connection.CreateWriteTransaction())
            {
                AppendHistory(transaction, context, state, appendToJob);
                transaction.Commit();
            }
        }

        private void AppendHistory(
            IAtomicWriteTransaction transaction, 
            StateContext context, 
            JobState state, 
            bool appendToJob)
        {
            var properties = new Dictionary<string, string>(
                state.GetProperties(context.JobMethod));
            var now = DateTime.UtcNow;

            properties.Add("State", state.StateName);
            
            if (appendToJob)
            {
                transaction.Jobs.SetState(context.JobId, state.StateName, properties);
            }

            properties.Add("Reason", state.Reason);
            properties.Add("CreatedAt", JobHelper.ToStringTimestamp(now));

            transaction.Jobs.AppendHistory(context.JobId, properties);
        }

        private JobFilterInfo GetFilters(JobMethod method)
        {
            return new JobFilterInfo(_getFiltersThunk(method));
        }
    }
}
