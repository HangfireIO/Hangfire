using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Server
{
    public class DelayedJobSchedulerFacts
    {
        private const string JobId = "id";
        private readonly Mock<JobStorageConnection> _connection;
        private readonly Mock<IBackgroundJobStateChanger> _stateChanger;
        private readonly BackgroundProcessContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly Mock<IDisposable> _distributedLock;

        public DelayedJobSchedulerFacts()
        {
            _context = new BackgroundProcessContextMock();

            _connection = new Mock<JobStorageConnection>();
            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _stateChanger = new Mock<IBackgroundJobStateChanger>();
            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            _distributedLock = new Mock<IDisposable>();
            _connection
                .Setup(x => x.AcquireDistributedLock("locks:schedulepoller", It.IsAny<TimeSpan>()))
                .Returns(_distributedLock.Object);

            _connection
                .SetupSequence(x => x.GetFirstByLowestScoreFromSet("schedule", 0, It.Is<double>(time => time > 0)))
                .Returns(JobId)
                .Throws(new OperationCanceledException("stopped"));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateChangerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new DelayedJobScheduler(Timeout.InfiniteTimeSpan, null));

            Assert.Equal("stateChanger", exception.ParamName);
        }

        [Fact]
        public void Execute_MovesJobStateToEnqueued()
        {
            RunSchedulerAndWait();

            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == JobId &&
                ctx.NewState is EnqueuedState &&
                ctx.ExpectedStates.SequenceEqual(new[] { ScheduledState.StateName }))));

            _connection.Verify(x => x.Dispose());
        }

        [Fact]
        public void Execute_MovesJobStateToEnqueued_UsingBatching_WhenAvailable()
        {
            // Arrange
            _connection
                .Setup(x => x.GetFirstByLowestScoreFromSet(null, It.IsAny<double>(), It.IsAny<double>(),It.IsAny<int>()))
                .Throws(new ArgumentNullException("key"));

            _connection
                .SetupSequence(x => x.GetFirstByLowestScoreFromSet("schedule", 0, It.Is<double>(time => time > 0), It.IsAny<int>()))
                .Returns(new List<string> { "job-1", "job-2" })
                .Throws(new OperationCanceledException("stopped"));

            // Act
            RunSchedulerAndWait();

            // Assert
            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == "job-1" &&
                ctx.NewState is EnqueuedState)));

            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == "job-2" &&
                ctx.NewState is EnqueuedState)));
        }

        [Fact]
        public void Execute_DoesNotUseBatching_WhenConnectionMethod_ThrowsAnException()
        {
            // Arrange
            _connection
                .Setup(x => x.GetFirstByLowestScoreFromSet(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(),It.IsAny<int>()))
                .Throws<NotImplementedException>();

            _connection
                .SetupSequence(x => x.GetFirstByLowestScoreFromSet("schedule", 0, It.Is<double>(time => time > 0)))
                .Returns("job-1")
                .Throws(new OperationCanceledException("stopped"));

            // Act
            RunSchedulerAndWait();

            // Assert
            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == "job-1" &&
                ctx.NewState is EnqueuedState)));
        }

        [Fact]
        public void Execute_DoesNotCallStateChanger_IfThereAreNoJobsToEnqueue()
        {
            _connection
                .SetupSequence(x => x.GetFirstByLowestScoreFromSet(
                "schedule", 0, It.Is<double>(time => time > 0)))
                .Returns((string)null)
                .Throws(new OperationCanceledException("stopped"));

            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _stateChanger.Verify(
                x => x.ChangeState(It.IsAny<StateChangeContext>()),
                Times.Never);
        }

        [Fact]
        public void Execute_RemovesAJobIdentifierFromTheSet_WhenStateChangeFails()
        {
            _stateChanger
                .Setup(x => x.ChangeState(It.IsAny<StateChangeContext>()))
                .Returns<IState>(null);

            RunSchedulerAndWait();

            _transaction.Verify(x => x.RemoveFromSet("schedule", JobId));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void Execute_ActsWithinADistributedLock()
        {
            RunSchedulerAndWait();

            _connection.Verify(x => x.AcquireDistributedLock(It.IsAny<string>(), It.IsAny<TimeSpan>()));
            _distributedLock.Verify(x => x.Dispose());
        }

        [Fact]
        public void Execute_DoesNotThrowDistributedLockTimeoutException()
        {
            _connection
                .Setup(x => x.AcquireDistributedLock("locks:schedulepoller", It.IsAny<TimeSpan>()))
                .Throws(new DistributedLockTimeoutException("locks:schedulepoller"));

            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);
        }

        private void RunSchedulerAndWait()
        {
            var scheduler = CreateScheduler();
            var exception = Assert.Throws<OperationCanceledException>(() => scheduler.Execute(_context.Object));

            Assert.Equal("stopped", exception.Message);
        }

        private DelayedJobScheduler CreateScheduler()
        {
            return new DelayedJobScheduler(TimeSpan.Zero, _stateChanger.Object);
        }
    }
}
