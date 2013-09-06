using System;
using System.Collections.Generic;
using System.Linq;

namespace HangFire
{
    /// <summary>
    /// Represents a top-level class for enqueuing jobs.
    /// </summary>
    public static class HangFireClient
    {
        private static readonly RedisClient Client = new RedisClient();
        private static readonly IList<IClientFilter> Filters = HangFireConfiguration.Current.ClientFilters;

        /// <summary>
        /// Puts specified job to the queue.
        /// </summary>
        /// <typeparam name="TJob">Job type</typeparam>
        public static string PerformAsync<TJob>()
        {
            return PerformAsync<TJob>(null);
        }

        public static string PerformAsync<TJob>(object args)
        {
            return PerformAsync(typeof(TJob), args);
        }

        public static string PerformAsync(Type jobType, object args = null)
        {
            if (jobType == null)
            {
                throw new ArgumentNullException("jobType");
            }

            var jobDescription = new JobDescription(jobType, args);

            Action enqueueAction = () =>
            {
                var serializedDescription = jobDescription.Serialize();
                var queue = JobHelper.GetQueueName(jobType);

                lock (Client)
                {
                    Client.TryToDo(storage => storage.EnqueueJob(queue, serializedDescription), throwOnError: true);
                }
            };

            InvokeFilters(jobDescription, enqueueAction);

            return jobDescription.Jid;
        }

        public static string PerformIn<TJob>(TimeSpan interval)
        {
            return PerformIn<TJob>(interval, null);
        }

        public static string PerformIn<TJob>(TimeSpan interval, object args)
        {
            return PerformIn(typeof(TJob), interval, args);
        }

        public static string PerformIn(Type jobType, TimeSpan interval, object args = null)
        {
            if (jobType == null)
            {
                throw new ArgumentNullException("jobType");
            }

            if (interval != interval.Duration())
            {
                throw new ArgumentOutOfRangeException("interval", "Interval value can not be negative.");
            }

            if (interval.Equals(TimeSpan.Zero))
            {
                return PerformAsync(jobType, args);
            }

            var at = DateTime.UtcNow.Add(interval).ToTimestamp();

            var jobDescription = new JobDescription(jobType, args);

            Action enqueueAction = () =>
            {
                var serializedDescription = jobDescription.Serialize();

                lock (Client)
                {
                    lock (Client)
                    {
                        Client.TryToDo(
                            storage => storage.ScheduleJob(serializedDescription, at),
                            throwOnError: true);
                    }
                }
            };

            InvokeFilters(jobDescription, enqueueAction);

            return jobDescription.Jid;
        }

        private static void InvokeFilters(
            JobDescription jobDescription, 
            Action enqueueAction)
        {
            var commandAction = enqueueAction;

            var entries = Filters.ToList();
            entries.Reverse();

            foreach (var entry in entries)
            {
                var currentEntry = entry;

                var filterContext = new ClientFilterContext(jobDescription, commandAction);
                commandAction = () => currentEntry.ClientFilter(filterContext);
            }

            commandAction();
        }

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static long ToTimestamp(this DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }
    }
}
