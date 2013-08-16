using System;

namespace HangFire
{
    public static class Perform
    {
        private static readonly RedisClient Client = new RedisClient();

        public static void Async<TWorker>()
            where TWorker : Worker
        {
            Async<TWorker>(null);
        }

        public static void Async<TWorker>(object args)
            where TWorker : Worker
        {
            var job = new Job(typeof(TWorker), args);

            InvokeInterceptors(job);
            if (job.Cancelled)
            {
                return;
            }

            // TODO: handle serialization exceptions.
            // Either properties or args can not be serialized if
            // it's type is unserializable. We need to throw this
            // exception to the client.
            var serialized = JsonHelper.Serialize(job);

            lock (Client)
            {
                Client.TryToDo(
                    redis => redis.EnqueueItemOnList("hangfire:queue:default", serialized),
                    reconnectOnNextUse: true);
            }
        }

        public static void In<TWorker>(TimeSpan interval)
        {
            In<TWorker>(interval, null);
        }

        public static void In<TWorker>(TimeSpan interval, object args)
        {
            var at = DateTime.UtcNow.Add(interval).ToTimestamp();

            var job = new Job(typeof(TWorker), args);
            InvokeInterceptors(job);
            if (job.Cancelled)
            {
                return;
            }

            // TODO: handle serialization exceptions.
            // Either properties or args can not be serialized if
            // it's type is unserializable. We need to throw this
            // exception to the client.
            var serialized = JsonHelper.Serialize(job);

            lock (Client)
            {
                Client.TryToDo(
                    redis => redis.AddItemToSortedSet("hangfire:schedule", serialized, at),
                    reconnectOnNextUse: true);
            }
        }

        private static void InvokeInterceptors(Job job)
        {
            var interceptors = Configuration.Instance.EnqueueInterceptors;

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
