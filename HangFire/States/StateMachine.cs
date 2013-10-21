using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Redis;

namespace HangFire.States
{
    public class StateMachine
    {
        private static readonly IDictionary<string, JobStateDescriptor> Descriptors
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

        internal static JobStateDescriptor GetStateDescriptor(
            string stateName)
        {
            return Descriptors.ContainsKey(stateName) ? Descriptors[stateName] : null;
        }

        private readonly IRedisClient _redis;
        private readonly IEnumerable<IStateChangedFilter> _stateChangedFilters;
        private readonly IEnumerable<IStateAppliedFilter> _stateAppliedFilters;

        public StateMachine(IRedisClient redis)
            : this(
                redis, 
                GlobalJobFilters.Filters.OfType<IStateChangedFilter>().ToList(),
                GlobalJobFilters.Filters.OfType<IStateAppliedFilter>().ToList())
        {
        }

        internal StateMachine(
            IRedisClient redis, 
            IEnumerable<IStateChangedFilter> stateChangedFilters,
            IEnumerable<IStateAppliedFilter> stateAppliedFilters)
        {
            if (redis == null) throw new ArgumentNullException("redis");
            if (stateChangedFilters == null) throw new ArgumentNullException("stateChangedFilters");
            if (stateAppliedFilters == null) throw new ArgumentNullException("stateAppliedFilters");

            _redis = redis;
            _stateChangedFilters = stateChangedFilters;
            _stateAppliedFilters = stateAppliedFilters;
        }

        public bool ChangeState(
            string jobId, JobState state, params string[] allowedCurrentStates)
        {
            if (state == null) throw new ArgumentNullException("state");

            using (_redis.AcquireLock(
                String.Format("hangfire:job:{0}:state-lock", jobId), TimeSpan.FromMinutes(1)))
            {
                foreach (var filter in _stateChangedFilters)
                {
                    var oldState = state;
                    state = filter.OnStateChanged(_redis, jobId, oldState);

                    if (oldState != state)
                    {
                        AppendHistory(jobId, oldState, false);
                    }
                }

                return ApplyState(jobId, state, allowedCurrentStates);
            }
        }

        private bool ApplyState(
            string jobId, 
            JobState state,
            params string[] allowedStates)
        {
            // TODO: what to do when job does not exists?
            var oldState = _redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", jobId), "State");

            if (allowedStates.Length > 0 && !allowedStates.Contains(oldState))
            {
                return false;
            }

            using (var transaction = _redis.CreateTransaction())
            {
                if (!String.IsNullOrEmpty(oldState))
                {
                    var descriptor = GetStateDescriptor(oldState);
                    if (descriptor != null)
                    {
                        descriptor.Unapply(transaction, jobId);
                    }

                    transaction.QueueCommand(x => x.RemoveEntry(
                        String.Format("hangfire:job:{0}:state", jobId)));

                    foreach (var filter in _stateAppliedFilters)
                    {
                        filter.OnStateUnapplied(transaction, jobId, oldState);
                    }
                }

                AppendHistory(transaction, jobId, state, true);

                state.Apply(transaction, jobId);

                foreach (var filter in _stateAppliedFilters)
                {
                    filter.OnStateApplied(transaction, jobId, state);
                }

                return transaction.Commit();
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
            var properties = state.GetProperties();
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
