using System;
using System.Collections.Generic;
using System.Linq;
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
            Descriptors.Add(stateName, descriptor);
        }

        private readonly IRedisClient _redis;
        private readonly IDictionary<string, JobStateDescriptor> _stateDescriptors;
        private readonly IEnumerable<IStateChangedFilter> _stateChangedFilters;
        private readonly IEnumerable<IStateAppliedFilter> _stateAppliedFilters;

        public StateMachine(IRedisClient redis)
            : this(
                redis, 
                Descriptors,
                GlobalJobFilters.Filters.OfType<IStateChangedFilter>().ToList(),
                GlobalJobFilters.Filters.OfType<IStateAppliedFilter>().ToList())
        {
        }

        internal StateMachine(
            IRedisClient redis, 
            IDictionary<string, JobStateDescriptor> stateDescriptors,
            IEnumerable<IStateChangedFilter> stateChangedFilters,
            IEnumerable<IStateAppliedFilter> stateAppliedFilters)
        {
            if (redis == null) throw new ArgumentNullException("redis");
            if (stateDescriptors == null) throw new ArgumentNullException("stateDescriptors");
            if (stateChangedFilters == null) throw new ArgumentNullException("stateChangedFilters");
            if (stateAppliedFilters == null) throw new ArgumentNullException("stateAppliedFilters");

            _redis = redis;
            _stateDescriptors = stateDescriptors;
            _stateChangedFilters = stateChangedFilters;
            _stateAppliedFilters = stateAppliedFilters;
        }

        public bool CreateInState(
            string jobId, IDictionary<string, string> parameters, JobState state)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format("hangfire:job:{0}", jobId),
                    parameters));

                foreach (var filter in _stateChangedFilters)
                {
                    var oldState = state;
                    state = filter.OnStateChanged(_redis, jobId, oldState);

                    if (oldState != state)
                    {
                        AppendHistory(transaction, jobId, oldState, false);
                    }
                }

                ApplyState(jobId, null, state, transaction);

                return transaction.Commit();
            }
        }

        public bool ChangeState(
            string jobId, JobState state, params string[] allowedCurrentStates)
        {
            if (state == null) throw new ArgumentNullException("state");

            using (_redis.AcquireLock(
                String.Format("hangfire:job:{0}:state-lock", jobId), TimeSpan.FromMinutes(1)))
            {
                var currentState = _redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", jobId), "State");

                if (currentState == null)
                {
                    return false;
                }

                if (allowedCurrentStates.Length > 0 && !allowedCurrentStates.Contains(currentState))
                {
                    return false;
                }

                foreach (var filter in _stateChangedFilters)
                {
                    var oldState = state;
                    state = filter.OnStateChanged(_redis, jobId, oldState);

                    if (oldState != state)
                    {
                        AppendHistory(jobId, oldState, false);
                    }
                }

                return ApplyState(jobId, currentState, state);
            }
        }

        private bool ApplyState(
            string jobId, 
            string currentState,
            JobState newState)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                ApplyState(jobId, currentState, newState, transaction);

                return transaction.Commit();
            }
        }

        private void ApplyState(string jobId, string currentState, JobState newState, IRedisTransaction transaction)
        {
            if (!String.IsNullOrEmpty(currentState))
            {
                if (_stateDescriptors.ContainsKey(currentState))
                {
                    _stateDescriptors[currentState].Unapply(transaction, jobId);
                }

                transaction.QueueCommand(x => x.RemoveEntry(
                    String.Format("hangfire:job:{0}:state", jobId)));

                foreach (var filter in _stateAppliedFilters)
                {
                    filter.OnStateUnapplied(transaction, jobId, currentState);
                }
            }

            AppendHistory(transaction, jobId, newState, true);

            newState.Apply(transaction, jobId);

            foreach (var filter in _stateAppliedFilters)
            {
                filter.OnStateApplied(transaction, jobId, newState);
            }
        }

        private void AppendHistory(
            string jobId, JobState state, bool appendToJob)
        {
            using (var transaction = _redis.CreateTransaction())
            {
                AppendHistory(transaction, jobId, state, appendToJob);
                transaction.Commit();
            }
        }

        private void AppendHistory(
            IRedisTransaction transaction, string jobId, JobState state, bool appendToJob)
        {
            var properties = new Dictionary<string, string>(state.GetProperties());
            var now = DateTime.UtcNow;

            properties.Add("State", state.StateName);

            if (appendToJob)
            {
                transaction.QueueCommand(x => x.SetEntryInHash(
                    String.Format("hangfire:job:{0}", jobId),
                    "State",
                    state.StateName));

                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format("hangfire:job:{0}:state", jobId),
                    properties));
            }

            properties.Add("Reason", state.Reason);
            properties.Add("CreatedAt", JobHelper.ToStringTimestamp(now));

            transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format("hangfire:job:{0}:history", jobId),
                JobHelper.ToJson(properties)));
        }
    }
}
