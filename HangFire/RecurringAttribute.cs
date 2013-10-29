using System;
using System.Linq;
using HangFire.Filters;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire
{
    public class RecurringAttribute : JobFilterAttribute, IStateChangingFilter
    {
        public RecurringAttribute(int seconds)
        {
            Seconds = seconds;
        }

        public int Seconds { get; private set; }

        public JobState OnStateChanging(IRedisClient redis, string jobId, JobState state)
        {
            if (redis == null) throw new ArgumentNullException("redis");
            if (state == null) throw new ArgumentNullException("state");

            if (state.StateName != SucceededState.Name)
            {
                return state;
            }

            var jobType = redis.GetValueFromHash(
                String.Format("hangfire:job:{0}", jobId),
                "Type");
            var type = Type.GetType(jobType);

            // TODO: check the type for null.
            var recurringAttribute = type.GetCustomAttributes(true).OfType<RecurringAttribute>().SingleOrDefault();

            if (recurringAttribute != null)
            {
                return new ScheduledState(
                    "Scheduled as a recurring job.",
                    DateTime.UtcNow.AddSeconds(recurringAttribute.Seconds));
            }

            return state;
        }
    }
}
