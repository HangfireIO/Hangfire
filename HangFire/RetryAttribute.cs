using System;
using HangFire.Filters;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire
{
    public class RetryAttribute : JobFilterAttribute, IStateChangingFilter
    {
        private int _attempts;
        private const int DefaultRetryAttempts = 3;

        public RetryAttribute()
        {
            Attempts = DefaultRetryAttempts;
        }

        public int Attempts
        {
            get { return _attempts; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "Attempts value must be equal or greater that zero.");
                }
                _attempts = value;
            }
        }

        public JobState OnStateChanging(
            JobDescriptor descriptor, JobState state, IRedisClient redis)
        {
            if (redis == null) throw new ArgumentNullException("redis");
            if (state == null) throw new ArgumentNullException("state");

            if (state.StateName != FailedState.Name)
            {
                // This filter accepts only failed job state.
                return state;
            }

            var retryCount = redis.IncrementValueInHash(
                String.Format("hangfire:job:{0}", descriptor.JobId),
                "RetryCount",
                1);

            if (retryCount <= Attempts)
            {
                var delay = DateTime.UtcNow.AddSeconds(SecondsToDelay(retryCount));

                // If attempt number is less than max attempts, we should
                // schedule the job to run again later.
                return new ScheduledState(
                    String.Format("Retry attempt {0} of {1}.", retryCount, Attempts), 
                    delay);
            }

            // When we exceeded the number of attempts, we should leave
            // the job in a failed state.
            return state;
        }

        // delayed_job uses the same basic formula
        private static int SecondsToDelay(long retryCount)
        {
            var random = new Random();
            return (int)Math.Round(
                Math.Pow(retryCount, 4) + 15 + (random.Next(30) * (retryCount + 1)));
        }
    }
}
