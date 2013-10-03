using System;
using HangFire.Filters;
using HangFire.Storage.States;
using ServiceStack.Redis;

namespace HangFire
{
    internal interface IJobStateFilter : IJobFilter
    {
        JobState OnJobState(IRedisClient redis, JobState state);
    }

    public class RetryJobsFilter : IJobStateFilter
    {
        private const int MaxRetryAttempts = 3;

        JobState IJobStateFilter.OnJobState(IRedisClient redis, JobState state)
        {
            if (state.StateName != FailedState.Name)
            {
                // This filter accepts only failed job state.
                return state;
            }

            var retryCount = redis.IncrementValueInHash(
                String.Format("hangfire:job:{0}", state.JobId),
                "RetryCount",
                1);

            if (retryCount < MaxRetryAttempts)
            {
                var jobType = redis.GetValueFromHash(
                    String.Format("hangfire:job:{0}", state.JobId),
                    "Type");

                var queueName = JobHelper.TryToGetQueueName(jobType);
                var delay = DateTime.UtcNow.AddSeconds(SecondsToDelay(retryCount));

                // If attempt number is less than max attempts, we should
                // schedule the job to run again later.
                return new ScheduledState(state.JobId, queueName, delay);
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
