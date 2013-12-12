// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
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
using HangFire.Client;
using HangFire.Filters;
using ServiceStack.Redis;

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

        private readonly IRedisClient _redis;
        private readonly IDictionary<string, JobStateDescriptor> _stateDescriptors;

        private readonly Func<JobInvocationData, IEnumerable<JobFilter>> _getFiltersThunk
            = JobFilterProviders.Providers.GetFilters;

        public StateMachine(IRedisClient redis)
            : this(
                redis,
                Descriptors,
                null)
        {
        }

        internal StateMachine(
            IRedisClient redis,
            IDictionary<string, JobStateDescriptor> stateDescriptors,
            IEnumerable<object> filters)
        {
            if (redis == null) throw new ArgumentNullException("redis");
            if (stateDescriptors == null) throw new ArgumentNullException("stateDescriptors");

            _redis = redis;
            _stateDescriptors = stateDescriptors;
            
            if (filters != null)
            {
                _getFiltersThunk = jd => filters.Select(f => new JobFilter(f, JobFilterScope.Type, null));
            }
        }
        
        public bool CreateInState(
            JobDescriptor descriptor,
            IDictionary<string, string> parameters,
            JobState state)
        {
            if (descriptor == null) throw new ArgumentNullException("descriptor");
            if (parameters == null) throw new ArgumentNullException("parameters");
            if (state == null) throw new ArgumentNullException("state");

            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format("hangfire:job:{0}", descriptor.JobId),
                    parameters));

                transaction.QueueCommand(x => x.ExpireEntryIn(
                    String.Format("hangfire:job:{0}", descriptor.JobId),
                    TimeSpan.FromHours(1)));

                transaction.Commit();
            }

            var filterInfo = GetFilters(descriptor.InvocationData);
            state = InvokeStateChangingFilters(descriptor, state, filterInfo.StateChangingFilters);

            using (var transaction = _redis.CreateTransaction())
            {
                ApplyState(descriptor, null, state, transaction, filterInfo.StateChangedFilters);

                transaction.QueueCommand(x =>
                    ((IRedisNativeClient)x).Persist(String.Format("hangfire:job:{0}", descriptor.JobId)));

                return transaction.Commit();
            }
        }

        public bool ChangeState(
            string jobId, JobState state, params string[] allowedCurrentStates)
        {
            if (String.IsNullOrWhiteSpace(jobId)) throw new ArgumentNullException("jobId");
            if (state == null) throw new ArgumentNullException("state");

            using (_redis.AcquireLock(
                String.Format("hangfire:job:{0}:state-lock", jobId), TimeSpan.FromMinutes(1)))
            {
                var job = _redis.GetAllEntriesFromHash(
                    String.Format("hangfire:job:{0}", jobId));

                if (job.Count == 0)
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
                    var invocationData = JobInvocationData.Deserialize(job);
                    var descriptor = new JobDescriptor(jobId, invocationData);

                    var filterInfo = GetFilters(invocationData);
                    state = InvokeStateChangingFilters(descriptor, state, filterInfo.StateChangingFilters);

                    return ApplyState(descriptor, currentState, state, filterInfo.StateChangedFilters);
                }
                catch (JobLoadException ex)
                {
                    // If the job type could not be loaded, we are unable to
                    // load corresponding filters, unable to process the job
                    // and sometimes unable to change its state (the enqueued
                    // state depends on the type of a job).

                    var descriptor = new JobDescriptor(jobId, null);

                    return ApplyState(
                        descriptor,
                        currentState,
                        new FailedState(
                            String.Format("Could not change the state of the job '{0}' to the '{1}'. See the inner exception for details.", state.StateName, descriptor.JobId),
                            ex),
                        Enumerable.Empty<IStateChangedFilter>());
                }
            }
        }

        private JobState InvokeStateChangingFilters(
            JobDescriptor descriptor, JobState state, IEnumerable<IStateChangingFilter> filters)
        {
            foreach (var filter in filters)
            {
                var oldState = state;
                state = filter.OnStateChanging(descriptor, oldState, _redis);

                if (oldState != state)
                {
                    AppendHistory(descriptor, oldState, false);
                }
            }
            return state;
        }

        private bool ApplyState(
            JobDescriptor descriptor,
            string currentState,
            JobState newState,
            IEnumerable<IStateChangedFilter> stateChangedFilters)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                ApplyState(descriptor, currentState, newState, transaction, stateChangedFilters);

                return transaction.Commit();
            }
        }

        private void ApplyState(
            JobDescriptor descriptor, 
            string currentState, 
            JobState newState, 
            IRedisTransaction transaction,
            IEnumerable<IStateChangedFilter> stateChangedFilters)
        {
            if (!String.IsNullOrEmpty(currentState))
            {
                if (_stateDescriptors.ContainsKey(currentState))
                {
                    _stateDescriptors[currentState].Unapply(descriptor, transaction);
                }

                transaction.QueueCommand(x => x.RemoveEntry(
                    String.Format("hangfire:job:{0}:state", descriptor.JobId)));

                foreach (var filter in stateChangedFilters)
                {
                    filter.OnStateUnapplied(descriptor, currentState, transaction);
                }
            }

            AppendHistory(transaction, descriptor, newState, true);

            newState.Apply(descriptor, transaction);

            foreach (var filter in stateChangedFilters)
            {
                filter.OnStateApplied(descriptor, newState, transaction);
            }
        }

        private void AppendHistory(
            JobDescriptor descriptor, JobState state, bool appendToJob)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                AppendHistory(transaction, descriptor, state, appendToJob);
                transaction.Commit();
            }
        }

        private void AppendHistory(
            IRedisTransaction transaction, 
            JobDescriptor descriptor, 
            JobState state, 
            bool appendToJob)
        {
            var properties = new Dictionary<string, string>(state.GetProperties(descriptor));
            var now = DateTime.UtcNow;

            properties.Add("State", state.StateName);

            if (appendToJob)
            {
                transaction.QueueCommand(x => x.SetEntryInHash(
                    String.Format("hangfire:job:{0}", descriptor.JobId),
                    "State",
                    state.StateName));

                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format("hangfire:job:{0}:state", descriptor.JobId),
                    properties));
            }

            properties.Add("Reason", state.Reason);
            properties.Add("CreatedAt", JobHelper.ToStringTimestamp(now));

            transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format("hangfire:job:{0}:history", descriptor.JobId),
                JobHelper.ToJson(properties)));
        }

        private JobFilterInfo GetFilters(JobInvocationData invocationData)
        {
            return new JobFilterInfo(_getFiltersThunk(invocationData));
        }
    }
}
