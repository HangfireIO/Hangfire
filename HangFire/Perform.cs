using System;

using BookSleeve;

namespace HangFire
{
    public static class Perform
    {
        private static readonly RedisConnection _redis = RedisClient.CreateConnection();

        public static void Async<TWorker>()
            where TWorker : Worker
        {
            Async<TWorker>(null);
        }

        public static void Async<TWorker>(object arg)
            where TWorker : Worker
        {
            var job = new Job(typeof(TWorker), arg);

            // TODO: handle serialization exceptions.
            // Either properties or args can not be serialized if
            // it's type is unserializable. We need to throw this
            // exception to the client.
            var serialized = JsonHelper.Serialize(job);

            // TODO: handle Redis exception. Do we need to try it again?
            var result = _redis.Lists.AddFirst(0, String.Format("queue:default"), serialized);
            _redis.Wait(result);
        }
    }
}
