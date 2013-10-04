using System;
using System.Collections.Generic;
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

        public virtual IDictionary<string, string> GetProperties()
        {
            return new Dictionary<string, string>();
        }

        private static readonly IDictionary<string, Descriptor> Descriptors
            = new Dictionary<string, Descriptor>();

        static JobState()
        {
            RegisterDescriptor(FailedState.Name, new FailedState.Descriptor());
            RegisterDescriptor(ProcessingState.Name, new ProcessingState.Descriptor());
            RegisterDescriptor(ScheduledState.Name, new ScheduledState.Descriptor());
            RegisterDescriptor(SucceededState.Name, new SucceededState.Descriptor());
            RegisterDescriptor(EnqueuedState.Name, new EnqueuedState.Descriptor());
        }

        public static void RegisterDescriptor(
            string stateName, Descriptor descriptor)
        {
            Descriptors.Add(stateName, descriptor);
        }

        public static bool Apply(IRedisClient redis, JobState state, params string[] allowedStates)
        {
            var filters = GlobalJobFilters.Filters.OfType<IJobStateFilter>().ToList();

            using (redis.AcquireLock(
                String.Format("hangfire:job:{0}:state-lock", state.JobId), TimeSpan.FromMinutes(1)))
            {
                foreach (var filter in filters)
                {
                    var oldState = state;
                    state = filter.OnStateChanged(redis, oldState);

                    if (oldState != state)
                    {
                        AppendHistory(redis, oldState, false);
                    }
                }

                return ApplyState(redis, state, filters, allowedStates);
            }
        }

        private static bool ApplyState(
            IRedisClient redis, JobState state, 
            IList<IJobStateFilter> filters, params string[] allowedStates)
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
                        var descriptor = Descriptors[oldState];
                        var properties = descriptor.GetPropertyKeys();

                        descriptor.Unapply(transaction, state.JobId);

                        // TODO: Use "HDEL key field1 field2 ..." command
                        foreach (var property in properties)
                        {
                            var name = property;

                            transaction.QueueCommand(x => x.RemoveEntryFromHash(
                                String.Format("hangfire:job:{0}", state.JobId),
                                name));
                        }
                    }

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
            properties.Add("State", state.StateName);
            properties.Add("Reason", state.Reason);
            properties.Add("Date", JobHelper.ToStringTimestamp(DateTime.UtcNow));

            if (appendToJob)
            {
                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format("hangfire:job:{0}", state.JobId),
                    properties));
            }

            transaction.QueueCommand(x => x.EnqueueItemOnList(
                String.Format("hangfire:job:{0}:history", state.JobId),
                JobHelper.ToJson(properties)));
        }

        public abstract class Descriptor
        {
            public virtual void Unapply(IRedisTransaction transaction, string jobId)
            {
            }

            public virtual IList<string> GetPropertyKeys()
            {
                return new List<string>();
            }
        }
    }
}
