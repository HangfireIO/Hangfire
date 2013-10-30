using System;
using HangFire.Filters;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire
{
    public class RecurringAttribute : JobFilterAttribute, IStateChangingFilter
    {
        public RecurringAttribute(int intervalInSeconds)
        {
            RepeatInterval = intervalInSeconds;
        }

        public int RepeatInterval { get; private set; }

        public JobState OnStateChanging(
            JobDescriptor descriptor, JobState state, IRedisClient redis)
        {
            if (redis == null) throw new ArgumentNullException("redis");
            if (state == null) throw new ArgumentNullException("state");

            if (state.StateName != SucceededState.Name)
            {
                return state;
            }

            return new ScheduledState(
                "Scheduled as a recurring job",
                DateTime.UtcNow.AddSeconds(RepeatInterval));
        }
    }
}
