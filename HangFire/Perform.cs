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
            Async(typeof(TWorker), args);
        }

        public static void Async(Type workerType, object args = null)
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
                    reconnectOnNextUse: true);
            }
        }

        public static void In<TWorker>(TimeSpan interval)
        {
            In<TWorker>(interval, null);
        }

        public static void In<TWorker>(TimeSpan interval, object args)
        {
            In(typeof(TWorker), interval, args);
        }

        public static void In(Type workerType, TimeSpan interval, object args = null)
        {
            if (workerType == null)
            {
                throw new ArgumentNullException("workerType");
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
                    reconnectOnNextUse: true);
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
