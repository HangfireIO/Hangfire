using System;
using ServiceStack.Redis;
using Xunit;

namespace Hangfire.Redis.Tests
{
    public class RedisTest : IDisposable
    {
        private readonly IRedisClient _redis;

        public RedisTest()
        {
            _redis = RedisUtils.CreateClient();
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        [Fact, CleanRedis]
        public void RedisSampleTest()
        {
            var defaultValue = _redis.GetValue("samplekey");
            Assert.Equal(null, defaultValue);
        }
    }
}
