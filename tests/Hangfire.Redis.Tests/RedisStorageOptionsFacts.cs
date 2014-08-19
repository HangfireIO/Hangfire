using System;
using Xunit;

namespace Hangfire.Redis.Tests
{
    public class RedisStorageOptionsFacts
    {
        [Fact]
        public void ConnectionPoolSize_HasDefaultValue()
        {
            var options = CreateOptions();
            Assert.Equal(50, options.ConnectionPoolSize);
        }

        [Fact]
        public void InvisibilityTimeout_HasDefaultValue()
        {
            var options = CreateOptions();
            Assert.Equal(TimeSpan.FromMinutes(30), options.InvisibilityTimeout);
        }

        private static RedisStorageOptions CreateOptions()
        {
            return new RedisStorageOptions();
        }
    }
}
