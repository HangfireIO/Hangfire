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

        private static readonly IDictionary<string, Action<IRedisTransaction, string>> UnapplyActions
            = new Dictionary<string, Action<IRedisTransaction, string>>();

        static JobState()
        {
            RegisterUnapplyAction(FailedState.Name, FailedState.Unapply);
            RegisterUnapplyAction(ProcessingState.Name, ProcessingState.Unapply);
            RegisterUnapplyAction(ScheduledState.Name, ScheduledState.Unapply);
            RegisterUnapplyAction(SucceededState.Name, SucceededState.Unapply);
        }

        public static void RegisterUnapplyAction(
            string stateName, Action<IRedisTransaction, string> unapplyAction)
        {
            UnapplyActions.Add(stateName, unapplyAction);
        }

        public static bool Apply(IRedisClient redis, JobState state, params string[] allowedStates)
        {
            var filters = GlobalJobFilters.Filters.OfType<IJobStateFilter>();

            // TODO: check lock boundaries
            using (redis.AcquireLock(
                String.Format("hangfire:job:{0}:state-lock", state.JobId), TimeSpan.FromMinutes(1)))
            {
                foreach (var filter in filters)
                {
                    var oldState = state;
                    state = filter.OnJobState(redis, oldState);

                    if (oldState != state)
                    {
                        AppendHistory(redis, oldState, false);
                    }
                }

                // TODO: add to history and apply the changes.
                return ApplyState(redis, state, allowedStates);
            }
        }

        private static bool ApplyState(IRedisClient redis, JobState state, params string[] allowedStates)
        {
            // TODO: what to do when transaction fails?
            // TODO: what to do when job does not exists?
            var oldState = redis.GetValueFromHash(String.Format("hangfire:job:{0}", state.JobId), "State");
            
            if (allowedStates.Length > 0 && !allowedStates.Contains(oldState))
            {
                return false;
            }

            using (var transaction = redis.CreateTransaction())
            {
                if (!String.IsNullOrEmpty(oldState) && UnapplyActions.ContainsKey(oldState))
                {
                    UnapplyActions[oldState](transaction, state.JobId);
                }

                AppendHistory(transaction, state, true);
                state.Apply(transaction);

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
    }
}
