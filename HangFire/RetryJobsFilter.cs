using System;
using HangFire.Filters;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire
{
    public class RetryJobsFilter : IStateChangingFilter
    {
        private const int MaxRetryAttempts = 3;

        public JobState OnStateChanging(IRedisClient redis, string jobId, JobState state)
        {
            if (redis == null) throw new ArgumentNullException("redis");
            if (state == null) throw new ArgumentNullException("state");

            if (state.StateName != FailedState.Name)
            {
                // This filter accepts only failed job state.
                return state;
            }

            var retryCount = redis.IncrementValueInHash(
                String.Format("hangfire:job:{0}", jobId),
                "RetryCount",
                1);

            if (retryCount <= MaxRetryAttempts)
            {
                var delay = DateTime.UtcNow.AddSeconds(SecondsToDelay(retryCount));

                // If attempt number is less than max attempts, we should
                // schedule the job to run again later.
                return new ScheduledState(
                    String.Format("Retry attempt {0} of {1}.", retryCount, MaxRetryAttempts), 
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
