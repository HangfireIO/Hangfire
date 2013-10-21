using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ServiceStack.Redis;

namespace HangFire.States
{
    public abstract class JobState
    {
        protected JobState(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; private set; }

        public abstract string StateName { get; }

        public abstract void Apply(IRedisTransaction transaction, string jobId);

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Method can return volatile data.")]
        public virtual IDictionary<string, string> GetProperties()
        {
            return new Dictionary<string, string>();
        }

        private static readonly IDictionary<string, JobStateDescriptor> Descriptors
            = new Dictionary<string, JobStateDescriptor>();

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static JobState()
        {
            RegisterDescriptor(FailedState.Name, new FailedState.Descriptor());
            RegisterDescriptor(ProcessingState.Name, new ProcessingState.Descriptor());
            RegisterDescriptor(ScheduledState.Name, new ScheduledState.Descriptor());
            RegisterDescriptor(SucceededState.Name, new SucceededState.Descriptor());
        }

        public static void RegisterDescriptor(
            string stateName, JobStateDescriptor descriptor)
        {
            Descriptors.Add(stateName, descriptor);
        }

        public static JobStateDescriptor GetDescriptor(
            string stateName)
        {
            return Descriptors.ContainsKey(stateName) ? Descriptors[stateName] : null;
        }

        public static bool Apply(
            IRedisClient redis, string jobId, JobState state, params string[] allowedStates)
        {
            if (redis == null) throw new ArgumentNullException("redis");
            if (state == null) throw new ArgumentNullException("state");

            var changingFilters = GlobalJobFilters.Filters
                .OfType<IStateChangedFilter>().ToList();

            using (redis.AcquireLock(
                String.Format("hangfire:job:{0}:state-lock", jobId), TimeSpan.FromMinutes(1)))
            {
                foreach (var filter in changingFilters)
                {
                    var oldState = state;
                    state = filter.OnStateChanged(redis, jobId, oldState);

                    if (oldState != state)
                    {
                        AppendHistory(redis, jobId, oldState, false);
                    }
                }

                var appliedFilters = GlobalJobFilters.Filters
                    .OfType<IStateAppliedFilter>().ToList();

                return ApplyState(redis, jobId, state, appliedFilters, allowedStates);
            }
        }

        private static bool ApplyState(
            IRedisClient redis, string jobId, JobState state, 
            IList<IStateAppliedFilter> filters, params string[] allowedStates)
        {
            // TODO: what to do when job does not exists?
            var oldState = redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", jobId), "State");
            
            if (allowedStates.Length > 0 && !allowedStates.Contains(oldState))
            {
                return false;
            }

            using (var transaction = redis.CreateTransaction())
            {
                if (!String.IsNullOrEmpty(oldState))
                {
                    var descriptor = GetDescriptor(oldState);
                    if (descriptor != null)
                    {
                        descriptor.Unapply(transaction, jobId);
                    }

                    transaction.QueueCommand(x => x.RemoveEntry(
                        String.Format("hangfire:job:{0}:state", jobId)));

                    foreach (var filter in filters)
                    {
                        filter.OnStateUnapplied(transaction, jobId, oldState);
                    }
                }

                AppendHistory(transaction, jobId, state, true);

                state.Apply(transaction, jobId);

                foreach (var filter in filters)
                {
                    filter.OnStateApplied(transaction, jobId, state);
                }

                return transaction.Commit();
            }
        }

        private static void AppendHistory(
            IRedisClient redis, string jobId, JobState state, bool appendToJob)
        {
            using (var transaction = redis.CreateTransaction())
            {
                AppendHistory(transaction, jobId, state, appendToJob);
                transaction.Commit();
            }
        }

        private static void AppendHistory(
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
