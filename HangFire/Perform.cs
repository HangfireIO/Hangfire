using System;

namespace HangFire
{
    public static class Perform
    {
        private static readonly RedisClient _client = new RedisClient();

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

            var redis = _client.GetConnection();
            var result = redis.Lists.AddFirst(0, String.Format("hangfire:queue:default"), serialized);
            redis.Wait(result);
        }
    }
}
