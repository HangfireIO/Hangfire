using System;
using System.Threading.Tasks;
using BookSleeve;

namespace HangFire
{
    internal static class Client
    {
        private static readonly RedisConnection _redis = RedisClient.CreateConnection();

        public static Task Enqueue(Type workerType, object arg)
        {
            var job = new Job(workerType, arg);
            var queue = "default";

            // TODO: handle serialization exceptions.
            // Either properties or args can not be serialized if
            // it's type is unserializable. We need to throw this
            // exception to the client.
            var serialized = JsonHelper.Serialize(job);

            // TODO: handle Redis exception. Do we need to try it again?
            return _redis.Lists.AddFirst(0, GetQueueKey(queue), serialized);
        }

        private static string GetQueueKey(string queueName)
        {
            return String.Format("queue:{0}", queueName);
        }
    }
}