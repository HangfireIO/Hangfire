using System.IO;

using ServiceStack.Redis;

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

        public static void Async<TWorker>(object arg)
            where TWorker : Worker
        {
            var job = new Job(typeof(TWorker), arg);

            // TODO: handle serialization exceptions.
            // Either properties or args can not be serialized if
            // it's type is unserializable. We need to throw this
            // exception to the client.
            var serialized = JsonHelper.Serialize(job);

            lock (Client)
            {
                try
                {
                    var redis = Client.Connection;
                    redis.EnqueueItemOnList("hangfire:queue:default", serialized);
                }
                catch (IOException)
                {
                    Client.Reconnect();
                    throw;
                }
                catch (RedisException)
                {
                    Client.Reconnect();
                    throw;
                }
            }
        }
    }
}
