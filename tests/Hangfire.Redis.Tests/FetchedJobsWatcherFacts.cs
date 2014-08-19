using System;
using System.Threading;
using Hangfire.Common;
using Xunit;

namespace Hangfire.Redis.Tests
{
    public class FetchedJobsWatcherFacts
    {
        private static readonly TimeSpan InvisibilityTimeout = TimeSpan.FromSeconds(10);

        private readonly RedisStorage _storage;
		private readonly CancellationTokenSource _cts;

        public FetchedJobsWatcherFacts()
        {
            _storage = new RedisStorage(RedisUtils.GetHostAndPort(), RedisUtils.GetDb());
			_cts = new CancellationTokenSource();
			_cts.Cancel();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new FetchedJobsWatcher(null, InvisibilityTimeout));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInvisibilityTimeoutIsZero()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => new FetchedJobsWatcher(_storage, TimeSpan.Zero));

            Assert.Equal("invisibilityTimeout", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInvisibilityTimeoutIsNegative()
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => new FetchedJobsWatcher(_storage, TimeSpan.FromSeconds(-1)));

            Assert.Equal("invisibilityTimeout", exception.ParamName);
        }

        [Fact, CleanRedis]
        public void Execute_EnqueuesTimedOutJobs_AndDeletesThemFromFetchedList()
        {
            using (var redis = RedisUtils.CreateClient())
            {
                // Arrange
                redis.AddItemToSet("hangfire:queues", "my-queue");
                redis.AddItemToList("hangfire:queue:my-queue:dequeued", "my-job");
                redis.SetEntryInHash("hangfire:job:my-job", "Fetched",
                    JobHelper.SerializeDateTime(DateTime.UtcNow.AddDays(-1)));

                var watcher = CreateWatcher();

                // Act
				watcher.Execute(_cts.Token);

                // Assert
                Assert.Equal(0, redis.GetListCount("hangfire:queue:my-queue:dequeued"));

                var listEntry = redis.DequeueItemFromList("hangfire:queue:my-queue");
                Assert.Equal("my-job", listEntry);

                var job = redis.GetAllEntriesFromHash("hangfire:job:my-job");
                Assert.False(job.ContainsKey("Fetched"));
            }
        }

        [Fact, CleanRedis]
        public void Execute_MarksDequeuedJobAsChecked_IfItHasNoFetchedFlagSet()
        {
            using (var redis = RedisUtils.CreateClient())
            {
                // Arrange
                redis.AddItemToSet("hangfire:queues", "my-queue");
                redis.AddItemToList("hangfire:queue:my-queue:dequeued", "my-job");

                var watcher = CreateWatcher();

                // Act
				watcher.Execute(_cts.Token);

                Assert.NotNull(JobHelper.DeserializeNullableDateTime(
                    redis.GetValueFromHash("hangfire:job:my-job", "Checked")));
            }
        }

        [Fact, CleanRedis]
        public void Execute_EnqueuesCheckedAndTimedOutJob_IfNoFetchedFlagSet()
        {
            using (var redis = RedisUtils.CreateClient())
            {
                // Arrange
                redis.AddItemToSet("hangfire:queues", "my-queue");
                redis.AddItemToList("hangfire:queue:my-queue:dequeued", "my-job");
                redis.SetEntryInHash("hangfire:job:my-job", "Checked",
                    JobHelper.SerializeDateTime(DateTime.UtcNow.AddDays(-1)));

                var watcher = CreateWatcher();

                // Act
				watcher.Execute(_cts.Token);

                // Arrange
                Assert.Equal(0, redis.GetListCount("hangfire:queue:my-queue:dequeued"));
                Assert.Equal(1, redis.GetListCount("hangfire:queue:my-queue"));

                var job = redis.GetAllEntriesFromHash("hangfire:job:my-job");
                Assert.False(job.ContainsKey("Checked"));
            }
        }

        [Fact, CleanRedis]
        public void Execute_DoesNotEnqueueTimedOutByCheckedFlagJob_IfFetchedFlagSet()
        {
            using (var redis = RedisUtils.CreateClient())
            {
                // Arrange
                redis.AddItemToSet("hangfire:queues", "my-queue");
                redis.AddItemToList("hangfire:queue:my-queue:dequeued", "my-job");
                redis.SetEntryInHash("hangfire:job:my-job", "Checked",
                    JobHelper.SerializeDateTime(DateTime.UtcNow.AddDays(-1)));
                redis.SetEntryInHash("hangfire:job:my-job", "Fetched",
                    JobHelper.SerializeDateTime(DateTime.UtcNow));

                var watcher = CreateWatcher();

                // Act
				watcher.Execute(_cts.Token);

                // Assert
                Assert.Equal(1, redis.GetListCount("hangfire:queue:my-queue:dequeued"));
            }
        }

        private FetchedJobsWatcher CreateWatcher()
        {
            return new FetchedJobsWatcher(_storage, InvisibilityTimeout);
        }
    }
}
