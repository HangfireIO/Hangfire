using Xunit;

namespace HangFire.Redis.Tests
{
    public class RedisStorageTests
    {
        [Fact, CleanRedis]
        public void DefaultCtor_InitializesCorrectDefaultValues()
        {
            var storage = new RedisStorage();

            Assert.Equal("localhost:6379", storage.HostAndPort);
            Assert.Equal(0, storage.Db);
        }
    }
}
