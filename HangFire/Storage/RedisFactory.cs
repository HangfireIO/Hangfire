using ServiceStack.Redis;

namespace HangFire.Storage
{
    internal class RedisFactory
    {
        public static IRedisClient Create()
        {
            var configuration = JobStorage.Configuration;
            return new RedisClient(
                configuration.RedisHost, 
                configuration.RedisPort,
                configuration.RedisPassword,
                configuration.RedisDb);
        }
    }
}
