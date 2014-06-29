using System.Linq;
using Xunit;

namespace Hangfire.Redis.Tests
{
    public class RedisStorageFacts
    {
        [Fact, CleanRedis]
        public void DefaultCtor_InitializesCorrectDefaultValues()
        {
            var storage = new RedisStorage();

            Assert.Equal("localhost:6379", storage.HostAndPort);
            Assert.Equal(0, storage.Db);
        }

        [Fact, CleanRedis]
        public void GetStateHandlers_ReturnsAllHandlers()
        {
            var storage = new RedisStorage();

            var handlers = storage.GetStateHandlers();

            var handlerTypes = handlers.Select(x => x.GetType()).ToArray();
            Assert.Contains(typeof(FailedStateHandler), handlerTypes);
            Assert.Contains(typeof(ProcessingStateHandler), handlerTypes);
            Assert.Contains(typeof(SucceededStateHandler), handlerTypes);
            Assert.Contains(typeof(DeletedStateHandler), handlerTypes);
        }

        private RedisStorage CreateStorage()
        {
            return new RedisStorage(RedisUtils.GetHostAndPort(), RedisUtils.GetDb());
        }
    }
}
