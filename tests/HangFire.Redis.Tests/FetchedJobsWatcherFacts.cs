using System;
using System.Threading;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Redis.Components;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Redis.Tests
{
    public class FetchedJobsWatcherFacts
    {
        private readonly RedisStorage _storage;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly Mock<IStateMachineFactory> _stateMachineFactory;
        private readonly CancellationToken _token;

        public FetchedJobsWatcherFacts()
        {
            _storage = new RedisStorage(RedisUtils.GetHostAndPort(), RedisUtils.GetDb());
            _token = new CancellationToken(true);

            _stateMachine = new Mock<IStateMachine>();
            _stateMachineFactory = new Mock<IStateMachineFactory>();

            _stateMachineFactory.Setup(x => x.Create(It.IsNotNull<IStorageConnection>()))
                .Returns(_stateMachine.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new FetchedJobsWatcher2(null, _stateMachineFactory.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new FetchedJobsWatcher2(_storage, null));

            Assert.Equal("stateMachineFactory", exception.ParamName);
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
                    JobHelper.ToStringTimestamp(DateTime.UtcNow.AddDays(-1)));

                var watcher = CreateWatcher();

                // Act
                watcher.Execute(_token);

                // Assert
                _stateMachine.Verify(x => x.TryToChangeState(
                    "my-job", 
                    It.IsAny<EnqueuedState>(),
                    new[] { EnqueuedState.StateName, ProcessingState.StateName }));

                Assert.Equal(0, redis.GetListCount("hangfire:queue:my-queue:dequeued"));
            }
        }

        [Fact]
        public void Execute_MarksDequeuedJobAsChecked_IfItHasNoFetchedFlagSet()
        {
            using (var redis = RedisUtils.CreateClient())
            {
                // Arrange
                redis.AddItemToSet("hangfire:queues", "my-queue");
                redis.AddItemToList("hangfire:queue:my-queue:dequeued", "my-job");

                var watcher = CreateWatcher();

                // Act
                watcher.Execute(_token);

                Assert.NotNull(JobHelper.FromNullableStringTimestamp(
                    redis.GetValueFromHash("hangfire:job:my-job", "Checked")));
            }
        }

        [Fact]
        public void Execute_EnqueuesCheckedAndTimedOutJob_IfNoFetchedFlagSet()
        {
            using (var redis = RedisUtils.CreateClient())
            {
                // Arrange
                redis.AddItemToSet("hangfire:queues", "my-queue");
                redis.AddItemToList("hangfire:queue:my-queue:dequeued", "my-job");
                redis.SetEntryInHash("hangfire:job:my-job", "Checked",
                    JobHelper.ToStringTimestamp(DateTime.UtcNow.AddDays(-1)));

                var watcher = CreateWatcher();

                // Act
                watcher.Execute(_token);

                // Arrange
                _stateMachine.Verify(x => x.TryToChangeState(
                    "my-job", It.IsAny<EnqueuedState>(), It.IsAny<string[]>()));
            }
        }

        [Fact]
        public void Execute_DoesNotEnqueueTimedOutByCheckedFlagJob_IfFetchedFlagSet()
        {
            using (var redis = RedisUtils.CreateClient())
            {
                // Arrange
                redis.AddItemToSet("hangfire:queues", "my-queue");
                redis.AddItemToList("hangfire:queue:my-queue:dequeued", "my-job");
                redis.SetEntryInHash("hangfire:job:my-job", "Checked",
                    JobHelper.ToStringTimestamp(DateTime.UtcNow.AddDays(-1)));
                redis.SetEntryInHash("hangfire:job:my-job", "Fetched",
                    JobHelper.ToStringTimestamp(DateTime.UtcNow));

                var watcher = CreateWatcher();

                // Act
                watcher.Execute(_token);

                // Assert
                _stateMachine.Verify(
                    x => x.TryToChangeState(It.IsAny<string>(), It.IsAny<State>(), It.IsAny<string[]>()),
                    Times.Never);
            }
        }

        private FetchedJobsWatcher2 CreateWatcher()
        {
            return new FetchedJobsWatcher2(_storage, _stateMachineFactory.Object);
        }
    }
}
