using System;
using System.Collections;
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
        public static void PerformAsync<TJob>()
        {
            PerformAsync<TJob>(null);
        }

        public static void PerformAsync<TJob>(object args)
        {
            PerformAsync(typeof(TJob), args);
        }

        public static void PerformAsync(Type jobType, object args = null)
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
        }

        public static void PerformIn<TJob>(TimeSpan interval)
        {
            PerformIn<TJob>(interval, null);
        }

        public static void PerformIn<TJob>(TimeSpan interval, object args)
        {
            PerformIn(typeof(TJob), interval, args);
        }

        public static void PerformIn(Type jobType, TimeSpan interval, object args = null)
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
                PerformAsync(jobType, args);
                return;
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
