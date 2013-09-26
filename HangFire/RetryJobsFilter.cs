using System;
using System.Collections.Generic;
using HangFire.Filters;

namespace HangFire
{
    public class RetryJobsFilter : IServerJobExceptionFilter
    {
        private const int MaxRetryAttempts = 10;

        public void OnServerException(ServerJobExceptionContext filterContext)
        {
            var descriptor = filterContext.JobDescriptor;
            long retryCount = 0;

            filterContext.Redis(x => retryCount = x.IncrementValueInHash(
                String.Format("hangfire:job:{0}", descriptor.JobId), 
                "RetryCount",
                1));
            
            if (retryCount < MaxRetryAttempts)
            {
                var delay = DateTime.UtcNow.AddSeconds(SecondsToDelay(retryCount));
                var timestamp = DateTimeToTimestamp(delay);

                var jobId = descriptor.JobId;
                var queueName = filterContext.ServerContext.QueueName;

                filterContext.Redis(redis =>
                    {
                        var transaction = redis.CreateTransaction();
                        transaction.QueueCommand(x => x.SetRangeInHash(
                            String.Format("hangfire:job:{0}", jobId),
                            new Dictionary<string, string>
                            {
                                { "ScheduledAt", JobHelper.ToJson(DateTime.UtcNow) },
                                { "ScheduledQueue", queueName }
                            }));

                        transaction.QueueCommand(x => x.AddItemToSortedSet(
                            "hangfire:schedule", jobId, timestamp));

                        transaction.Commit();
                    });

                filterContext.ExceptionHandled = true;    
            }
        }

        // delayed_job uses the same basic formula
        private static int SecondsToDelay(long retryCount)
        {
            var random = new Random();
            return (int)Math.Round(
                Math.Pow(retryCount, 4) + 15 + (random.Next(30) * (retryCount + 1)));
        }

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static long DateTimeToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }
    }
}
