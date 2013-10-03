using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Redis;

namespace HangFire.Storage.States
{
    internal class JobStateArgs
    {
        public JobStateArgs(string jobId)
        {
            JobId = jobId;
        }

        public string JobId { get; private set; }
    }

    internal abstract class JobState
    {
        private static readonly IDictionary<string, JobState> RegisteredStates =
            new Dictionary<string, JobState>();
        private static readonly IDictionary<Type, JobState> StatesByTypes =
            new Dictionary<Type, JobState>();

        static JobState()
        {
            RegisterState(new EnqueuedState());
            RegisterState(new ScheduledState());
            RegisterState(new ProcessingState());
            RegisterState(new SucceededState());
            RegisterState(new FailedState());
        }

        public static void RegisterState(JobState state)
        {
            RegisteredStates.Add(state.StateName, state);
            StatesByTypes.Add(state.GetType(), state);
        }

        public static JobState Find(string state)
        {
            return RegisteredStates[state];
        }

        public static T Find<T>()
            where T : JobState
        {
            return (T)StatesByTypes[typeof (T)];
        }

        public abstract string StateName { get; }

        public void Unapply(IRedisTransaction transaction, string jobId)
        {
            UnapplyCore(transaction, jobId);
        }

        protected abstract void UnapplyCore(IRedisTransaction transaction, string jobId);
    }

    internal abstract class JobState<T> : JobState
        where T : JobStateArgs
    {
        protected abstract void ApplyCore(IRedisTransaction transaction, T args);

        protected virtual IDictionary<string, string> GetProperties(T args)
        {
            return new Dictionary<string, string>();
        }

        public bool Apply(IRedisClient redis, T args, params JobState[] allowedStates)
        {
            // TODO: what to do when transaction fails?
            // TODO: what to do when job does not exists?
            redis.Watch(String.Format("hangfire:job:{0}", args.JobId));
            var oldState = redis.GetValueFromHash(String.Format("hangfire:job:{0}", args.JobId), "State");

            var allowed = allowedStates.Select(x => x.StateName).ToArray();
            if (allowed.Length > 0 && !allowed.Contains(oldState))
            {
                redis.UnWatch();
                return false;
            }
            
            using (var transaction = redis.CreateTransaction())
            {
                if (!String.IsNullOrEmpty(oldState))
                {
                    Find(oldState).Unapply(transaction, args.JobId);
                }

                var properties = GetProperties(args);

                if (properties.Count > 0)
                {
                    transaction.QueueCommand(x => x.SetRangeInHash(
                        String.Format("hangfire:job:{0}", args.JobId),
                        properties));
                }

                ApplyCore(transaction, args);

                transaction.QueueCommand(x => x.SetEntryInHash(String.Format("hangfire:job:{0}", args.JobId), "State", StateName));

                return transaction.Commit();
            }
        }
    }
}
