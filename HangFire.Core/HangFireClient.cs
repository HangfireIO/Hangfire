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
            where TJob : HangFireJob
        {
            PerformAsync<TJob>(null);
        }

        public static void PerformAsync<TJob>(object args)
            where TJob : HangFireJob
        {
            PerformAsync(typeof(TJob), args);
        }

        public static void PerformAsync(Type jobType, object args = null)
        {
            if (jobType == null)
            {
                throw new ArgumentNullException("jobType");
            }

            if (!typeof(HangFireJob).IsAssignableFrom(jobType))
            {
                throw new ArgumentException(
                    String.Format("Job type must be a descendant of the '{0}' class", typeof(HangFireJob).Name));
            }

            var serializedJob = InterceptAndSerializeJob(jobType, args);
            if (serializedJob == null)
            {
                return;
            }

            var queue = HangFireJob.GetQueueName(jobType);

            lock (Client)
            {
                Client.TryToDo(
                    storage => storage.EnqueueJob(queue, serializedJob),
                    throwOnError: true);
            }
        }

        public static void PerformIn<TJob>(TimeSpan interval)
            where TJob : HangFireJob
        {
            PerformIn<TJob>(interval, null);
        }

        public static void PerformIn<TJob>(TimeSpan interval, object args)
            where TJob : HangFireJob
        {
            PerformIn(typeof(TJob), interval, args);
        }

        public static void PerformIn(Type jobType, TimeSpan interval, object args = null)
        {
            if (jobType == null)
            {
                throw new ArgumentNullException("jobType");
            }

            if (!typeof(HangFireJob).IsAssignableFrom(jobType))
            {
                throw new ArgumentException(
                    String.Format("Job type must be a descendant of the '{0}' class", typeof(HangFireJob).Name));
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
                filter.InterceptEnqueue(jobDescription);
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
