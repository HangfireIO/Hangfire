using System;
using System.Linq;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire
{
    public class RecurringAttribute : Attribute
    {
        public RecurringAttribute(int seconds)
        {
            Seconds = seconds;
        }

        public int Seconds { get; private set; }
    }

    public class RecurringJobsFilter : IJobStateFilter
    {
        public JobState OnStateChanged(IRedisClient redis, JobState state)
        {
            if (state.StateName != SucceededState.Name)
            {
                return state;
            }

            var jobType = redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", state.JobId),
                "Type");
            var type = Type.GetType(jobType);

            // TODO: check the type for null.
            var recurringAttribute = type.GetCustomAttributes(true).OfType<RecurringAttribute>().SingleOrDefault();

            if (recurringAttribute != null)
            {
                var queueName = JobHelper.GetQueueName(type);

                return new ScheduledState(
                    state.JobId, 
                    "Scheduled as a recurring job.",
                    queueName, 
                    DateTime.UtcNow.AddSeconds(recurringAttribute.Seconds));
            }

            return state;
        }

        public void OnStateApplied(IRedisTransaction transaction, JobState state)
        {
        }

        public void OnStateUnapplied(IRedisTransaction transaction, string state)
        {
        }
    }
}
