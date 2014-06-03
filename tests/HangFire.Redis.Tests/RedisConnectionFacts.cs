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

        [Fact, CleanRedis]
        public void GetAllItemsFromList_ThrowsAnException_IfKeyIsNull()
        {
            UseConnection(connection => 
                Assert.Throws<ArgumentNullException>(() => connection.GetAllItemsFromList(null)));
        }

        [Fact, CleanRedis]
        public void GetAllItemsFromList_ReturnsEmptyArray_IfListDoesNotExist()
        {
            UseConnection(connection =>
            {
                var result = connection.GetAllItemsFromList("some-list");

                Assert.NotNull(result);
                Assert.Equal(0, result.Length);
            });
        }

        [Fact, CleanRedis]
        public void GetAllItemsFromList_ReturnsAllItems_WithinTheSpecifiedOrder()
        {
            UseConnections((redis, connection) =>
            {
                // Fuck, I open the https://github.com/ServiceStack/ServiceStack.Redis/blob/master/src/ServiceStack.Redis/RedisClient_List.cs
                // every time I use the `IRedisClient` interface.

                // Arrange
                redis.AddItemToList("hangfire:some-list", "1");
                redis.AddItemToList("hangfire:some-list", "2");
                redis.AddItemToList("hangfire:some-list", "3");

                // Act
                var result = connection.GetAllItemsFromList("some-list");

                // Assert
                Assert.Equal(3, result.Length);
                Assert.Equal("1", result[0]);
                Assert.Equal("2", result[1]);
                Assert.Equal("3", result[2]);
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
