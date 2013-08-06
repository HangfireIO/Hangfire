using System;
using ServiceStack.Redis;

namespace HangFire
{
    internal class Client : IDisposable
    {
        private readonly IRedisClient _redis;

        public Client(IRedisClient redis)
        {
            _redis = redis;
        }

        public void Enqueue(Type workerType, object arg)
        {
            var job = new Job(workerType, arg);
            var queue = "default";

            // TODO: handle serialization exceptions.
            // Either properties or args can not be serialized if
            // it's type is unserializable. We need to throw this
            // exception to the client.
            var serialized = JsonHelper.Serialize(job);

            // TODO: handle Redis exception. Do we need to try it again?
            _redis.PrependItemToList(GetQueueKey(queue), serialized);
        }

        private static string GetQueueKey(string queueName)
        {
            return String.Format("queue:{0}", queueName);
        }

        public void Dispose()
        {
            _redis.Dispose();
        }
    }
}