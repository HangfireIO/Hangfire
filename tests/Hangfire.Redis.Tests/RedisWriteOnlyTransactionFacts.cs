using System;
using System.Collections.Generic;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using ServiceStack.Redis;
using Xunit;

namespace Hangfire.Redis.Tests
{
    public class RedisWriteOnlyTransactionFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_WhenTransactionIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new RedisWriteOnlyTransaction(null));
        }

        [Fact, CleanRedis]
        public void ExpireJob_SetsExpirationDateForAllRelatedKeys()
        {
            UseConnection(redis =>
            {
                // Arrange
                redis.SetEntry("hangfire:job:my-job", "job");
                redis.SetEntry("hangfire:job:my-job:state", "state");
                redis.SetEntry("hangfire:job:my-job:history", "history");

                // Act
                Commit(redis, x => x.ExpireJob("my-job", TimeSpan.FromDays(1)));

                // Assert
                var jobEntryTtl = redis.GetTimeToLive("hangfire:job:my-job");
                var stateEntryTtl = redis.GetTimeToLive("hangfire:job:my-job:state");
                var historyEntryTtl = redis.GetTimeToLive("hangfire:job:my-job:state");

                Assert.True(TimeSpan.FromHours(23) < jobEntryTtl && jobEntryTtl < TimeSpan.FromHours(25));
                Assert.True(TimeSpan.FromHours(23) < stateEntryTtl && stateEntryTtl < TimeSpan.FromHours(25));
                Assert.True(TimeSpan.FromHours(23) < historyEntryTtl && historyEntryTtl < TimeSpan.FromHours(25));
            });
        }

        [Fact, CleanRedis]
        public void SetJobState_ModifiesJobEntry()
        {
            UseConnection(redis =>
            {
                // Arrange
                var state = new Mock<IState>();
                state.Setup(x => x.SerializeData()).Returns(new Dictionary<string, string>());
                state.Setup(x => x.Name).Returns("my-state");

                // Act
                Commit(redis, x => x.SetJobState("my-job", state.Object));

                // Assert
                var hash = redis.GetAllEntriesFromHash("hangfire:job:my-job");
                Assert.Equal("my-state", hash["State"]);
            });
        }

        [Fact, CleanRedis]
        public void SetJobState_RewritesStateEntry()
        {
            UseConnection(redis =>
            {
                // Arrange
                redis.SetEntryInHash("hangfire:job:my-job:state", "OldName", "OldValue");

                var state = new Mock<IState>();
                state.Setup(x => x.SerializeData()).Returns(
                    new Dictionary<string, string>
                    {
                        { "Name", "Value" }
                    });
                state.Setup(x => x.Name).Returns("my-state");
                state.Setup(x => x.Reason).Returns("my-reason");

                // Act
                Commit(redis, x => x.SetJobState("my-job", state.Object));

                // Assert
                var stateHash = redis.GetAllEntriesFromHash("hangfire:job:my-job:state");
                Assert.False(stateHash.ContainsKey("OldName"));
                Assert.Equal("my-state", stateHash["State"]);
                Assert.Equal("my-reason", stateHash["Reason"]);
                Assert.Equal("Value", stateHash["Name"]);
            });
        }

        [Fact, CleanRedis]
        public void SetJobState_AppendsJobHistoryList()
        {
            UseConnection(redis =>
            {
                // Arrange
                var state = new Mock<IState>();
                state.Setup(x => x.Name).Returns("my-state");
                state.Setup(x => x.SerializeData()).Returns(new Dictionary<string, string>());

                // Act
                Commit(redis, x => x.SetJobState("my-job", state.Object));

                // Assert
                Assert.Equal(1, redis.GetListCount("hangfire:job:my-job:history"));
            });
        }

        [Fact, CleanRedis]
        public void PersistJob_RemovesExpirationDatesForAllRelatedKeys()
        {
            UseConnection(redis =>
            {
                // Arrange
                redis.SetEntry("hangfire:job:my-job", "job", TimeSpan.FromDays(1));
                redis.SetEntry("hangfire:job:my-job:state", "state", TimeSpan.FromDays(1));
                redis.SetEntry("hangfire:job:my-job:history", "history", TimeSpan.FromDays(1));

                // Act
                Commit(redis, x => x.PersistJob("my-job"));

                // Assert
                Assert.True(redis.GetTimeToLive("hangfire:job:my-job") < TimeSpan.Zero);
                Assert.True(redis.GetTimeToLive("hangfire:job:my-job:state") < TimeSpan.Zero);
                Assert.True(redis.GetTimeToLive("hangfire:job:my-job:history") < TimeSpan.Zero);
            });
        }

        [Fact, CleanRedis]
        public void AddJobState_AddsJobHistoryEntry_AsJsonObject()
        {
            UseConnection(redis =>
            {
                // Arrange
                var state = new Mock<IState>();
                state.Setup(x => x.Name).Returns("my-state");
                state.Setup(x => x.Reason).Returns("my-reason");
                state.Setup(x => x.SerializeData()).Returns(
                    new Dictionary<string, string> { { "Name", "Value" } });

                // Act
                Commit(redis, x => x.AddJobState("my-job", state.Object));

                // Assert
                var serializedEntry = redis.GetItemFromList("hangfire:job:my-job:history", 0);
                Assert.NotNull(serializedEntry);

                var entry = JobHelper.FromJson<Dictionary<string, string>>(serializedEntry);
                Assert.Equal("my-state", entry["State"]);
                Assert.Equal("my-reason", entry["Reason"]);
                Assert.Equal("Value", entry["Name"]);
                Assert.True(entry.ContainsKey("CreatedAt"));
            });
        }

        [Fact, CleanRedis]
        public void AddToQueue_AddsSpecifiedJobToTheQueue()
        {
            UseConnection(redis =>
            {
                Commit(redis, x => x.AddToQueue("critical", "my-job"));

                Assert.True(redis.SetContainsItem("hangfire:queues", "critical"));
                Assert.Equal("my-job", redis.GetItemFromList("hangfire:queue:critical", 0));
            });
        }

        [Fact, CleanRedis]
        public void AddToQueue_PrependsListWithJob()
        {
            UseConnection(redis =>
            {
                redis.EnqueueItemOnList("hangfire:queue:critical", "another-job");

                Commit(redis, x => x.AddToQueue("critical", "my-job"));

                Assert.Equal("my-job", redis.GetItemFromList("hangfire:queue:critical", 0));
            });
        }

        [Fact, CleanRedis]
        public void IncrementCounter_IncrementValueEntry()
        {
            UseConnection(redis =>
            {
                redis.SetEntry("hangfire:entry", "3");

                Commit(redis, x => x.IncrementCounter("entry"));

                Assert.Equal("4", redis.GetValue("hangfire:entry"));
                Assert.True(redis.GetTimeToLive("hangfire:entry") < TimeSpan.Zero);
            });
        }

        [Fact, CleanRedis]
        public void IncrementCounter_WithExpiry_IncrementsValueAndSetsExpirationDate()
        {
            UseConnection(redis =>
            {
                redis.SetEntry("hangfire:entry", "3");

                Commit(redis, x => x.IncrementCounter("entry", TimeSpan.FromDays(1)));

                var entryTtl = redis.GetTimeToLive("hangfire:entry");
                Assert.Equal("4", redis.GetValue("hangfire:entry"));
                Assert.True(TimeSpan.FromHours(23) < entryTtl && entryTtl < TimeSpan.FromHours(25));
            });
        }

        [Fact, CleanRedis]
        public void DecrementCounter_DecrementsTheValueEntry()
        {
            UseConnection(redis =>
            {
                redis.SetEntry("hangfire:entry", "3");

                Commit(redis, x => x.DecrementCounter("entry"));

                Assert.Equal("2", redis.GetValue("hangfire:entry"));
                Assert.True(redis.GetTimeToLive("entry") < TimeSpan.Zero);
            });
        }

        [Fact, CleanRedis]
        public void DecrementCounter_WithExpiry_DecrementsTheValueAndSetsExpirationDate()
        {
            UseConnection(redis =>
            {
                redis.SetEntry("hangfire:entry", "3");

                Commit(redis, x => x.DecrementCounter("entry", TimeSpan.FromDays(1)));

                var entryTtl = redis.GetTimeToLive("hangfire:entry");
                Assert.Equal("2", redis.GetValue("hangfire:entry"));
                Assert.True(TimeSpan.FromHours(23) < entryTtl && entryTtl < TimeSpan.FromHours(25));
            });
        }

        [Fact, CleanRedis]
        public void AddToSet_AddsItemToSortedSet()
        {
            UseConnection(redis =>
            {
                Commit(redis, x => x.AddToSet("my-set", "my-value"));

                Assert.True(redis.SortedSetContainsItem("hangfire:my-set", "my-value"));
            });
        }

        [Fact, CleanRedis]
        public void AddToSet_WithScore_AddsItemToSortedSetWithScore()
        {
            UseConnection(redis =>
            {
                Commit(redis, x => x.AddToSet("my-set", "my-value", 3.2));

                Assert.True(redis.SortedSetContainsItem("hangfire:my-set", "my-value"));
                Assert.Equal(3.2, redis.GetItemScoreInSortedSet("hangfire:my-set", "my-value"), 3);
            });
        }

        [Fact, CleanRedis]
        public void RemoveFromSet_RemoveSpecifiedItemFromSortedSet()
        {
            UseConnection(redis =>
            {
                redis.AddItemToSortedSet("hangfire:my-set", "my-value");

                Commit(redis, x => x.RemoveFromSet("my-set", "my-value"));

                Assert.False(redis.SortedSetContainsItem("hangfire:my-set", "my-value"));
            });
        }

        [Fact, CleanRedis]
        public void InsertToList_PrependsListWithSpecifiedValue()
        {
            UseConnection(redis =>
            {
                redis.AddItemToList("hangfire:list", "value");

                Commit(redis, x => x.InsertToList("list", "new-value"));

                Assert.Equal("new-value", redis.GetItemFromList("hangfire:list", 0));
            });
        }

        [Fact, CleanRedis]
        public void RemoveFromList_RemovesAllGivenValuesFromList()
        {
            UseConnection(redis =>
            {
                redis.AddItemToList("hangfire:list", "value");
                redis.AddItemToList("hangfire:list", "another-value");
                redis.AddItemToList("hangfire:list", "value");

                Commit(redis, x => x.RemoveFromList("list", "value"));

                Assert.Equal(1, redis.GetListCount("hangfire:list"));
                Assert.Equal("another-value", redis.GetItemFromList("hangfire:list", 0));
            });
        }

        [Fact, CleanRedis]
        public void TrimList_TrimsListToASpecifiedRange()
        {
            UseConnection(redis =>
            {
                redis.AddItemToList("hangfire:list", "1");
                redis.AddItemToList("hangfire:list", "2");
                redis.AddItemToList("hangfire:list", "3");
                redis.AddItemToList("hangfire:list", "4");

                Commit(redis, x => x.TrimList("list", 1, 2));

                Assert.Equal(2, redis.GetListCount("hangfire:list"));
                Assert.Equal("2", redis.GetItemFromList("hangfire:list", 0));
                Assert.Equal("3", redis.GetItemFromList("hangfire:list", 1));
            });
        }

        [Fact, CleanRedis]
        public void SetRangeInHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseConnection(redis =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(redis, x => x.SetRangeInHash(null, new Dictionary<string, string>())));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Fact, CleanRedis]
        public void SetRangeInHash_ThrowsAnException_WhenKeyValuePairsArgumentIsNull()
        {
            UseConnection(redis =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(redis, x => x.SetRangeInHash("some-hash", null)));

                Assert.Equal("keyValuePairs", exception.ParamName);
            });
        }

        [Fact, CleanRedis]
        public void SetRangeInHash_SetsAllGivenKeyPairs()
        {
            UseConnection(redis =>
            {
                Commit(redis, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    { "Key1", "Value1" },
                    { "Key2", "Value2" }
                }));

                var hash = redis.GetAllEntriesFromHash("hangfire:some-hash");
                Assert.Equal("Value1", hash["Key1"]);
                Assert.Equal("Value2", hash["Key2"]);
            });
        }

        [Fact, CleanRedis]
        public void RemoveHash_ThrowsAnException_WhenKeyIsNull()
        {
            UseConnection(redis =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => Commit(redis, x => x.RemoveHash(null)));
            });
        }

        [Fact, CleanRedis]
        public void RemoveHash_RemovesTheCorrespondingEntry()
        {
            UseConnection(redis =>
            {
                redis.SetEntryInHash("hangfire:some-hash", "key", "value");

                Commit(redis, x => x.RemoveHash("some-hash"));

                var hash = redis.GetAllEntriesFromHash("hangfire:some-hash");
                Assert.Equal(0, hash.Count);
            });
        }

        private static void Commit(IRedisClient redis, Action<RedisWriteOnlyTransaction> action)
        {
            using (var transaction = new RedisWriteOnlyTransaction(redis.CreateTransaction()))
            {
                action(transaction);
                transaction.Commit();
            }
        }

        private static void UseConnection(Action<IRedisClient> action)
        {
            using (var redis = RedisUtils.CreateClient())
            {
                action(redis);
            }
        }
    }
}
