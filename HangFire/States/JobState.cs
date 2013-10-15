using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ServiceStack.Redis;

namespace HangFire.States
{
    public abstract class JobState
    {
        protected JobState(string jobId, string reason)
        {
            JobId = jobId;
            Reason = reason;
        }

        public string JobId { get; private set; }
        public string Reason { get; private set; }

        public abstract string StateName { get; }

        public abstract void Apply(IRedisTransaction transaction);

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
            RegisterDescriptor(FailedState.Name, new FailedStateDescriptor());
            RegisterDescriptor(ProcessingState.Name, new ProcessingStateDescriptor());
            RegisterDescriptor(ScheduledState.Name, new ScheduledStateDescriptor());
            RegisterDescriptor(SucceededState.Name, new SucceededStateDescriptor());
        }

        public static void RegisterDescriptor(
            string stateName, JobStateDescriptor descriptor)
        {
            Descriptors.Add(stateName, descriptor);
        }

        public static bool Apply(
            IRedisClient redis, JobState state, params string[] allowedStates)
        {
            if (redis == null) throw new ArgumentNullException("redis");
            if (state == null) throw new ArgumentNullException("state");

            var changingFilters = GlobalJobFilters.Filters
                .OfType<IStateChangedFilter>().ToList();

            using (redis.AcquireLock(
                String.Format("hangfire:job:{0}:state-lock", state.JobId), TimeSpan.FromMinutes(1)))
            {
                foreach (var filter in changingFilters)
                {
                    var oldState = state;
                    state = filter.OnStateChanged(redis, oldState);

                    if (oldState != state)
                    {
                        AppendHistory(redis, oldState, false);
                    }
                }

                var appliedFilters = GlobalJobFilters.Filters
                    .OfType<IStateAppliedFilter>().ToList();

                return ApplyState(redis, state, appliedFilters, allowedStates);
            }
        }

        private static bool ApplyState(
            IRedisClient redis, JobState state, 
            IList<IStateAppliedFilter> filters, params string[] allowedStates)
        {
            // TODO: what to do when job does not exists?
            var oldState = redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", state.JobId), "State");
            
            if (allowedStates.Length > 0 && !allowedStates.Contains(oldState))
            {
                return false;
            }

            using (var transaction = redis.CreateTransaction())
            {
                if (!String.IsNullOrEmpty(oldState))
                {
                    if (Descriptors.ContainsKey(oldState))
                    {
                        Descriptors[oldState].Unapply(transaction, state.JobId);
                    }

                    transaction.QueueCommand(x => x.RemoveEntry(
                        String.Format("hangfire:job:{0}:state", state.JobId)));

                    foreach (var filter in filters)
                    {
                        filter.OnStateUnapplied(transaction, oldState);
                    }
                }

                AppendHistory(transaction, state, true);

                state.Apply(transaction);

                foreach (var filter in filters)
                {
                    filter.OnStateApplied(transaction, state);
                }

                return transaction.Commit();
            }
        }

        private static void AppendHistory(
            IRedisClient redis, JobState state, bool appendToJob)
        {
            using (var transaction = redis.CreateTransaction())
            {
                AppendHistory(transaction, state, appendToJob);
                transaction.Commit();
            }
        }

        private static void AppendHistory(
            IRedisTransaction transaction, JobState state, bool appendToJob)
        {
            var properties = state.GetProperties();
            var now = DateTime.UtcNow;
            
            if (appendToJob)
            {
                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format("hangfire:job:{0}", state.JobId),
                    new Dictionary<string, string>
                        {
                            { "State", state.StateName },
                        }));

                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format("hangfire:job:{0}:state", state.JobId),
                    properties));
            }

            properties.Add("State", state.StateName);
            properties.Add("Reason", state.Reason);
            properties.Add("CreatedAt", JobHelper.ToStringTimestamp(now));

            transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format("hangfire:job:{0}:history", state.JobId),
                JobHelper.ToJson(properties)));
        }
    }
}
