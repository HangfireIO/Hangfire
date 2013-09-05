using System;

namespace HangFire
{
    /// <summary>
    /// Represents a top-level class for enqueuing jobs.
    /// </summary>
    public static class HangFireClient
    {
        private static readonly RedisClient Client = new RedisClient();

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

            var serializedJob = InterceptAndSerializeJob(jobType, args);
            if (serializedJob == null)
            {
                return;
            }

            var queue = JobHelper.GetQueueName(jobType);

            lock (Client)
            {
                Client.TryToDo(
                    storage => storage.EnqueueJob(queue, serializedJob),
                    throwOnError: true);
            }
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

            var serializedJob = InterceptAndSerializeJob(jobType, args);
            if (serializedJob == null)
            {
                return;
            }

            lock (Client)
            {
                Client.TryToDo(
                    storage => storage.ScheduleJob(serializedJob, at),
                    throwOnError: true);
            }
        }

        private static string InterceptAndSerializeJob(Type workerType, object args)
        {
            var job = new JobDescription(workerType, args);
            InvokeFilters(job);
            if (job.Canceled)
            {
                return null;
            }

            // TODO: handle serialization exceptions.
            // Either properties or args can not be serialized if
            // it's type is unserializable. We need to throw this
            // exception to the client.
            return JsonHelper.Serialize(job);
        }

        private static void InvokeFilters(JobDescription jobDescription)
        {
            var filters = HangFireConfiguration.Current.ClientFilters;

            foreach (var filter in filters)
            {
                filter.ClientFilter(new ClientFilterContext(jobDescription));
            }
        }

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static long ToTimestamp(this DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }
    }
}
