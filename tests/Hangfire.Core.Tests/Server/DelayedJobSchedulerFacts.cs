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
        private readonly List<string> _schedule = new List<string>();

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
                .Setup(x => x.GetFirstByLowestScoreFromSet("schedule", 0, It.Is<double>(time => time > 0)))
                .Returns(_schedule.FirstOrDefault);
            
            _connection
                .Setup(x => x.GetFirstByLowestScoreFromSet("schedule", 0, It.Is<double>(time => time > 0), It.IsAny<int>()))
                .Returns(_schedule.ToList);

            _stateChanger
                .Setup(x => x.ChangeState(It.IsNotNull<StateChangeContext>()))
                .Callback<StateChangeContext>(ctx =>
                {
                    if (!(ctx.NewState is ScheduledState)) _schedule.Remove(ctx.BackgroundJobId);
                });

            _transaction
                .Setup(x => x.RemoveFromSet("schedule", It.IsNotNull<string>()))
                .Callback<string, string>((key, value) => _schedule.Remove(value));
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
            var scheduler = CreateScheduler();
            _schedule.Add(JobId);
            
            scheduler.Execute(_context.Object);

            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == JobId &&
                ctx.NewState is EnqueuedState &&
                ctx.ExpectedStates.SequenceEqual(new[] { ScheduledState.StateName }) &&
                ctx.DisableFilters == false)));

            _connection.Verify(x => x.Dispose());
        }

        [Fact]
        public void Execute_MovesJobStateToEnqueued_UsingBatching_WhenAvailable()
        {
            // Arrange
            EnableBatching();

            _schedule.Add("job-1");
            _schedule.Add("job-2");

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

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
            
            _schedule.Add("job-1");

            _connection
                .Setup(x => x.GetFirstByLowestScoreFromSet("schedule", 0, It.Is<double>(time => time > 0)))
                .Returns(_schedule.FirstOrDefault);

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == "job-1" &&
                ctx.NewState is EnqueuedState)));
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_DoesNotCallStateChanger_IfThereAreNoJobsToEnqueue(bool batching)
        {
            if (batching) EnableBatching();
            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _stateChanger.Verify(
                x => x.ChangeState(It.IsAny<StateChangeContext>()),
                Times.Never);
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_RemovesAJobIdentifierFromTheSet_WhenStateChangeFails(bool batching)
        {
            if (batching) EnableBatching();
            _stateChanger
                .Setup(x => x.ChangeState(It.IsAny<StateChangeContext>()))
                .Returns<IState>(null);
            _schedule.Add(JobId);

            var scheduler = CreateScheduler();
            
            scheduler.Execute(_context.Object);

            _transaction.Verify(x => x.RemoveFromSet("schedule", JobId));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_MovesJobToTheFailedState_WithFiltersDisabled_WhenStateChangerThrowsAnException(bool batching)
        {
            // Arrange
            if (batching) EnableBatching();

            _schedule.Add(JobId);
            _stateChanger
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is EnqueuedState)))
                .Throws<InvalidOperationException>();

            var scheduler = CreateScheduler();
            
            // Act
            scheduler.Execute(_context.Object);
            
            // Assert
            _stateChanger.Verify(
                x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is EnqueuedState)),
                Times.AtLeast(3));
            
            _stateChanger.Verify(
                x => x.ChangeState(It.Is<StateChangeContext>(ctx => 
                    ctx.BackgroundJobId == JobId &&
                    ctx.NewState is FailedState &&
                    ((FailedState)ctx.NewState).Exception.GetType() == typeof(InvalidOperationException) &&
                    ctx.ExpectedStates.Contains(ScheduledState.StateName) &&
                    ctx.DisableFilters == true)),
                Times.Once);
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_AbleToProcessFurtherJobs_WhenStateChangerThrowsAnException_ForPreviousOnes(bool batching)
        {
            // Arrange
            if (batching) EnableBatching();

            _schedule.Add(JobId);
            _schedule.Add("AnotherId");

            _stateChanger
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.BackgroundJobId == JobId && ctx.NewState is ScheduledState)))
                .Throws<InvalidOperationException>();

            var scheduler = CreateScheduler();
            
            // Act
            scheduler.Execute(_context.Object);
            
            // Assert
            _stateChanger.Verify(
                x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.BackgroundJobId == "AnotherId" && ctx.NewState is EnqueuedState)),
                Times.Once);
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_ActsWithinADistributedLock(bool batching)
        {
            if (batching) EnableBatching();
            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _connection.Verify(x => x.AcquireDistributedLock(It.IsAny<string>(), It.IsAny<TimeSpan>()));
            _distributedLock.Verify(x => x.Dispose());
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_DoesNotThrowDistributedLockTimeoutException(bool batching)
        {
            if (batching) EnableBatching();
            _connection
                .Setup(x => x.AcquireDistributedLock("locks:schedulepoller", It.IsAny<TimeSpan>()))
                .Throws(new DistributedLockTimeoutException("locks:schedulepoller"));

            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_RemovesJobFromSchedule_WhenIdDoesNotExists(bool batching)
        {
            // Arrange
            if (batching) EnableBatching();
            _schedule.Add(JobId);

            _connection.Setup(x => x.GetJobData(JobId)).Returns<JobData>(null);

            _stateChanger
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is EnqueuedState)))
                .Returns<IState>(null);

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _stateChanger.Verify(x => x.ChangeState(It.IsAny<StateChangeContext>()), Times.Once);
            _transaction.Verify(x => x.RemoveFromSet("schedule", JobId));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_RemovesJobFromSchedule_WhenJobIsNotInScheduledState(bool batching)
        {
            // Arrange
            if (batching) EnableBatching();
            _schedule.Add(JobId);

            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData { State = SucceededState.StateName });

            _stateChanger
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is EnqueuedState)))
                .Returns<IState>(null);

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _stateChanger.Verify(x => x.ChangeState(It.IsAny<StateChangeContext>()), Times.Once);
            _transaction.Verify(x => x.RemoveFromSet("schedule", JobId));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_DoesNotRemoveJobFromSchedule_WhenJobIsInTheScheduledState(bool batching)
        {
            // Arrange
            if (batching) EnableBatching();
            _schedule.Add(JobId);

            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData { State = ScheduledState.StateName });

            _stateChanger
                .SetupSequence(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is EnqueuedState)))
                .Returns((IState)null)
                .Returns(() => { _schedule.Remove(JobId); return new EnqueuedState(); });

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(x => x.RemoveFromSet("schedule", JobId), Times.Never);
            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        private DelayedJobScheduler CreateScheduler()
        {
            return new DelayedJobScheduler(TimeSpan.Zero, _stateChanger.Object);
        }
        
        private void EnableBatching()
        {
            _connection
                .Setup(x => x.GetFirstByLowestScoreFromSet(null, It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>()))
                .Throws(new ArgumentNullException("key"));
        }
    }
}
