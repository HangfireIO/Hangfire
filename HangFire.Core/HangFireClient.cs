using System;

namespace HangFire
{
    public static class HangFireClient
    {
        private static readonly RedisClient Client = new RedisClient();

        public static void PerformAsync<TWorker>()
            where TWorker : Worker
        {
            PerformAsync<TWorker>(null);
        }

        public static void PerformAsync<TWorker>(object args)
            where TWorker : Worker
        {
            PerformAsync(typeof(TWorker), args);
        }

        public static void PerformAsync(Type workerType, object args = null)
        {
            if (workerType == null)
            {
                throw new ArgumentNullException("workerType");
            }

            var serializedJob = InterceptAndSerializeJob(workerType, args);
            if (serializedJob == null)
            {
                return;
            }

            var queue = Worker.GetQueueName(workerType);

            lock (Client)
            {
                Client.TryToDo(
                    storage => storage.EnqueueJob(queue, serializedJob),
                    throwOnError: true);
            }
        }

        public static void PerformIn<TWorker>(TimeSpan interval)
        {
            PerformIn<TWorker>(interval, null);
        }

        public static void PerformIn<TWorker>(TimeSpan interval, object args)
        {
            PerformIn(typeof(TWorker), interval, args);
        }

        public static void PerformIn(Type workerType, TimeSpan interval, object args = null)
        {
            if (workerType == null)
            {
                throw new ArgumentNullException("workerType");
            }

            if (interval != interval.Duration())
            {
                throw new ArgumentOutOfRangeException("interval", "Interval value can not be negative.");
            }

            var at = DateTime.UtcNow.Add(interval).ToTimestamp();

            var serializedJob = InterceptAndSerializeJob(workerType, args);
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
            var job = new Job(workerType, args);
            InvokeInterceptors(job);
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

        private static void InvokeInterceptors(Job job)
        {
            var interceptors = HangFireConfiguration.Current.ClientFilters;

            foreach (var interceptor in interceptors)
            {
                interceptor.InterceptEnqueue(job);
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
