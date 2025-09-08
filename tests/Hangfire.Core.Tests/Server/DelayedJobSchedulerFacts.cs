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
        private const int Sequential = 1;
        private const int Parallel = 4;
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
                .Returns(() =>
                {
                    lock (_schedule) return _schedule.FirstOrDefault();
                });
            
            _connection
                .Setup(x => x.GetFirstByLowestScoreFromSet("schedule", 0, It.Is<double>(time => time > 0), It.IsAny<int>()))
                .Returns(() =>
                {
                    lock (_schedule) return _schedule.ToList();
                });

            _stateChanger
                .Setup(x => x.ChangeState(It.IsNotNull<StateChangeContext>()))
                .Callback<StateChangeContext>(ctx =>
                {
                    if (!(ctx.NewState is ScheduledState)) lock (_schedule) _schedule.Remove(ctx.BackgroundJobId);
                });

            _transaction
                .Setup(x => x.RemoveFromSet("schedule", It.IsNotNull<string>()))
                .Callback<string, string>((key, value) =>
                {
                    lock (_schedule) _schedule.Remove(value);
                });
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateChangerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new DelayedJobScheduler(Timeout.InfiniteTimeSpan, null));

            Assert.Equal("stateChanger", exception.ParamName);
        }

        [Theory]
        [InlineData(false, Sequential)]
        [InlineData(false, Parallel)]
        [InlineData(true,  Sequential)]
        [InlineData(true,  Parallel)]
        public void Execute_MovesJobStateToEnqueued(bool batching, int maxParallelism)
        {
            EnableBatching(batching);

            var scheduler = CreateScheduler(maxParallelism);
            lock (_schedule) _schedule.Add(JobId);
            
            scheduler.Execute(_context.Object);

            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == JobId &&
                ctx.NewState is EnqueuedState &&
                ctx.ExpectedStates.SequenceEqual(new[] { ScheduledState.StateName }) &&
                ctx.DisableFilters == false)));

            _connection.Verify(x => x.Dispose());
        }

        [Theory]
        [InlineData(false, Sequential)]
        [InlineData(false, Parallel)]
        [InlineData(true,  Sequential)]
        [InlineData(true,  Parallel)]
        public void Execute_EnqueuesJobIdDirectly_AndRemovesItFromSchedule_WhenTargetQueueIsEncodedIntoTheSetEntry(bool batching, int maxParallelism)
        {
            EnableBatching(batching);

            // Arrange
            lock (_schedule)
            {
                _schedule.Add("default:some-id");
                _schedule.Add("critical:another-id");
            }

            var scheduler = CreateScheduler(maxParallelism);

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(x => x.AddToQueue("default", "some-id"));
            _transaction.Verify(x => x.RemoveFromSet("schedule", "default:some-id"));
            _transaction.Verify(x => x.AddToQueue("critical", "another-id"));
            _transaction.Verify(x => x.RemoveFromSet("schedule", "critical:another-id"));

            _transaction.Verify(x => x.Commit(), Times.Exactly(batching ? 1 : 2));

            _stateChanger.Verify(x => x.ChangeState(It.IsAny<StateChangeContext>()), Times.Never);
        }

        [Theory]
        [InlineData(Sequential)][InlineData(Parallel)]
        public void Execute_MovesJobStateToEnqueued_UsingBatching_WhenAvailable(int maxParallelism)
        {
            // Arrange
            EnableBatching(true);

            lock (_schedule)
            {
                _schedule.Add("job-1");
                _schedule.Add("job-2");
            }

            var scheduler = CreateScheduler(maxParallelism);

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

        [Theory]
        [InlineData(Sequential)][InlineData(Parallel)]
        public void Execute_WithBatching_EnqueuesJobIdDirectly_AndRemovesItFromSchedule_WhenTargetQueueIsEncodedIntoTheSetEntry(int maxParallelism)
        {
            // Arrange
            EnableBatching(true);

            lock (_schedule)
            {
                _schedule.Add("default:some-id");
                _schedule.Add("critical:another-id");
            }

            var scheduler = CreateScheduler(maxParallelism);

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(x => x.AddToQueue("default", "some-id"));
            _transaction.Verify(x => x.AddToQueue("critical", "another-id"));
            _transaction.Verify(x => x.RemoveFromSet("schedule", "default:some-id"));
            _transaction.Verify(x => x.RemoveFromSet("schedule", "critical:another-id"));

            _transaction.Verify(x => x.Commit(), Times.Once);

            _stateChanger.Verify(x => x.ChangeState(It.IsAny<StateChangeContext>()), Times.Never);
        }

        [Theory]
        [InlineData(Sequential)][InlineData(Parallel)]
        public void Execute_DoesNotUseBatching_WhenConnectionMethod_ThrowsAnException(int maxParallelism)
        {
            // Arrange
            _connection
                .Setup(x => x.GetFirstByLowestScoreFromSet(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(),It.IsAny<int>()))
                .Throws<NotImplementedException>();
            
            lock (_schedule) _schedule.Add("job-1");

            _connection
                .Setup(x => x.GetFirstByLowestScoreFromSet("schedule", 0, It.Is<double>(time => time > 0)))
                .Returns(() =>
                {
                    lock (_schedule) return _schedule.FirstOrDefault();
                });

            var scheduler = CreateScheduler(maxParallelism);

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == "job-1" &&
                ctx.NewState is EnqueuedState)));
        }

        [Theory]
        [InlineData(false, Sequential)]
        [InlineData(false, Parallel)]
        [InlineData(true,  Sequential)]
        [InlineData(true,  Parallel)]
        public void Execute_DoesNotCallStateChanger_IfThereAreNoJobsToEnqueue(bool batching, int maxParallelism)
        {
            EnableBatching(batching);
            var scheduler = CreateScheduler(maxParallelism);

            scheduler.Execute(_context.Object);

            _stateChanger.Verify(
                x => x.ChangeState(It.IsAny<StateChangeContext>()),
                Times.Never);
        }

        [Theory]
        [InlineData(false, Sequential)]
        [InlineData(false, Parallel)]
        [InlineData(true,  Sequential)]
        [InlineData(true,  Parallel)]
        public void Execute_RemovesAJobIdentifierFromTheSet_WhenStateChangeFails(bool batching, int maxParallelism)
        {
            EnableBatching(batching);
            _stateChanger
                .Setup(x => x.ChangeState(It.IsAny<StateChangeContext>()))
                .Returns<IState>(null);
            lock (_schedule) _schedule.Add(JobId);

            var scheduler = CreateScheduler(maxParallelism);
            
            scheduler.Execute(_context.Object);

            _transaction.Verify(x => x.RemoveFromSet("schedule", JobId));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false, Sequential)]
        [InlineData(false, Parallel)]
        [InlineData(true,  Sequential)]
        [InlineData(true,  Parallel)]
        public void Execute_MovesJobToTheFailedState_WithFiltersDisabled_WhenStateChangerThrowsAnException(bool batching, int maxParallelism)
        {
            // Arrange
            EnableBatching(batching);

            lock (_schedule) _schedule.Add(JobId);
            _stateChanger
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is EnqueuedState)))
                .Throws<InvalidOperationException>();

            var scheduler = CreateScheduler(maxParallelism);
            scheduler.RetryDelayFunc = _ => TimeSpan.FromMilliseconds(50);

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
        [InlineData(false, Sequential)]
        [InlineData(false, Parallel)]
        [InlineData(true,  Sequential)]
        [InlineData(true,  Parallel)]
        public void Execute_AbleToProcessFurtherJobs_WhenStateChangerThrowsAnException_ForPreviousOnes(bool batching, int maxParallelism)
        {
            // Arrange
            EnableBatching(batching);

            lock (_schedule)
            {
                _schedule.Add(JobId);
                _schedule.Add("AnotherId");
            }

            _stateChanger
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.BackgroundJobId == JobId && ctx.NewState is ScheduledState)))
                .Throws<InvalidOperationException>();

            var scheduler = CreateScheduler(maxParallelism);
            
            // Act
            scheduler.Execute(_context.Object);
            
            // Assert
            _stateChanger.Verify(
                x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.BackgroundJobId == "AnotherId" && ctx.NewState is EnqueuedState)),
                Times.Once);
        }

        [Theory]
        [InlineData(false, Sequential)]
        [InlineData(false, Parallel)]
        [InlineData(true,  Sequential)]
        [InlineData(true,  Parallel)]
        public void Execute_ActsWithinADistributedLock(bool batching, int maxParallelism)
        {
            EnableBatching(batching);
            var scheduler = CreateScheduler(maxParallelism);

            scheduler.Execute(_context.Object);

            _connection.Verify(x => x.AcquireDistributedLock(It.IsAny<string>(), It.IsAny<TimeSpan>()));
            _distributedLock.Verify(x => x.Dispose());
        }

        [Theory]
        [InlineData(false, Sequential)]
        [InlineData(false, Parallel)]
        [InlineData(true,  Sequential)]
        [InlineData(true,  Parallel)]
        public void Execute_DoesNotThrowDistributedLockTimeoutException(bool batching, int maxParallelism)
        {
            EnableBatching(batching);
            _connection
                .Setup(x => x.AcquireDistributedLock("locks:schedulepoller", It.IsAny<TimeSpan>()))
                .Throws(new DistributedLockTimeoutException("locks:schedulepoller"));

            var scheduler = CreateScheduler(maxParallelism);

            scheduler.Execute(_context.Object);
        }

        [Theory]
        [InlineData(false, Sequential)]
        [InlineData(false, Parallel)]
        [InlineData(true,  Sequential)]
        [InlineData(true,  Parallel)]
        public void Execute_RemovesJobFromSchedule_WhenIdDoesNotExists(bool batching, int maxParallelism)
        {
            // Arrange
            EnableBatching(batching);
            lock (_schedule) _schedule.Add(JobId);

            _connection.Setup(x => x.GetJobData(JobId)).Returns<JobData>(null);

            _stateChanger
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is EnqueuedState)))
                .Returns<IState>(null);

            var scheduler = CreateScheduler(maxParallelism);

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _stateChanger.Verify(x => x.ChangeState(It.IsAny<StateChangeContext>()), Times.Once);
            _transaction.Verify(x => x.RemoveFromSet("schedule", JobId));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false, Sequential)]
        [InlineData(false, Parallel)]
        [InlineData(true,  Sequential)]
        [InlineData(true,  Parallel)]
        public void Execute_RemovesJobFromSchedule_WhenJobIsNotInScheduledState(bool batching, int maxParallelism)
        {
            // Arrange
            EnableBatching(batching);
            lock (_schedule) _schedule.Add(JobId);

            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData { State = SucceededState.StateName });

            _stateChanger
                .Setup(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is EnqueuedState)))
                .Returns<IState>(null);

            var scheduler = CreateScheduler(maxParallelism);

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _stateChanger.Verify(x => x.ChangeState(It.IsAny<StateChangeContext>()), Times.Once);
            _transaction.Verify(x => x.RemoveFromSet("schedule", JobId));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false, Sequential)]
        [InlineData(false, Parallel)]
        [InlineData(true,  Sequential)]
        [InlineData(true,  Parallel)]
        public void Execute_DoesNotRemoveJobFromSchedule_WhenJobIsInTheScheduledState(bool batching, int maxParallelism)
        {
            // Arrange
            EnableBatching(batching);
            lock (_schedule) _schedule.Add(JobId);

            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData { State = ScheduledState.StateName });

            _stateChanger
                .SetupSequence(x => x.ChangeState(It.Is<StateChangeContext>(ctx => ctx.NewState is EnqueuedState)))
                .Returns((IState)null)
                .Returns(() => { lock (_schedule) _schedule.Remove(JobId); return new EnqueuedState(); });

            var scheduler = CreateScheduler(maxParallelism);

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(x => x.RemoveFromSet("schedule", JobId), Times.Never);
            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        private DelayedJobScheduler CreateScheduler(int maxParallelism)
        {
            return new DelayedJobScheduler(TimeSpan.Zero, _stateChanger.Object)
            {
                MaxDegreeOfParallelism = maxParallelism
            };
        }
        
        private void EnableBatching(bool value)
        {
            if (value)
            {
                _connection
                    .Setup(x => x.GetFirstByLowestScoreFromSet(null, It.IsAny<double>(), It.IsAny<double>(),
                        It.IsAny<int>()))
                    .Throws(new ArgumentNullException("key"));
            }
        }
    }
}
