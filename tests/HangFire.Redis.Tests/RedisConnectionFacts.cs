using System;
using System.Collections.Generic;
using ServiceStack.Redis;
using Xunit;

namespace HangFire.Redis.Tests
{
    public class RedisConnectionFacts
    {
        [Fact, CleanRedis]
        public void GetStateData_ThrowsAnException_WhenJobIdIsNull()
        {
            UseConnection(
                connection => Assert.Throws<ArgumentNullException>(
                    () => connection.GetStateData(null)));
        }

        [Fact, CleanRedis]
        public void GetStateData_ReturnsNull_WhenJobDoesNotExist()
        {
            UseConnection(connection =>
            {
                var result = connection.GetStateData("random-id");
                Assert.Null(result);
            });
        }

        [Fact, CleanRedis]
        public void GetStateData_ReturnsCorrectResult()
        {
            UseConnections((redis, connection) =>
            {
                redis.SetRangeInHash(
                    "hangfire:job:my-job:state",
                    new Dictionary<string, string>
                    {
                        { "State", "Name" },
                        { "Reason", "Reason" },
                        { "Key", "Value" }
                    });

                var result = connection.GetStateData("my-job");

                Assert.NotNull(result);
                Assert.Equal("Name", result.Name);
                Assert.Equal("Reason", result.Reason);
                Assert.Equal("Value", result.Data["Key"]);
            });
        }

        private void UseConnections(Action<IRedisClient, RedisConnection> action)
        {
            using (var redis = RedisUtils.CreateClient())
            using (var connection = new RedisConnection(redis))
            {
                action(redis, connection);
            }
        }

        private void UseConnection(Action<RedisConnection> action)
        {
            using (var connection = new RedisConnection(RedisUtils.CreateClient()))
            {
                action(connection);
            }
        }
    }
}
