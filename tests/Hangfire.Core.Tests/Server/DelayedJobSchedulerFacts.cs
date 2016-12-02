using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IBackgroundJobStateChanger> _stateChanger;
        private readonly BackgroundProcessContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly Mock<IDisposable> _distributedLock;
        private readonly string[] _queues;
        private readonly Dictionary<string, double> _jobSet;

        public DelayedJobSchedulerFacts()
        {
            _context = new BackgroundProcessContextMock();
            _context.CancellationTokenSource.Cancel();

            _connection = new Mock<IStorageConnection>();
            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _stateChanger = new Mock<IBackgroundJobStateChanger>();
            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            _distributedLock = new Mock<IDisposable>();

            _jobSet = new Dictionary<string, double>()
            {
                { JobId, 1 }
            };

            _connection.Setup(x => x.GetAllValuesWithScoresFromSetQueueWithinScoreRange(
                "schedule", "default", 0, It.Is<double>(time => time > 0))).Returns(_jobSet);

            _queues = new[] { EnqueuedState.DefaultQueue };
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateChangerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new DelayedJobScheduler(Timeout.InfiniteTimeSpan, null));

            Assert.Equal("stateChanger", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueuesIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new DelayedJobScheduler(Timeout.InfiniteTimeSpan, _stateChanger.Object, null));

            Assert.Equal("queues", exception.ParamName);
        }

        [Fact]
        public void Execute_MovesJobStateToEnqueued_InDefaultQueue()
        {
            var scheduler = CreateScheduler();

			scheduler.Execute(_context.Object);

            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == JobId &&
                ctx.NewState is EnqueuedState &&
                ((EnqueuedState)ctx.NewState).Queue == EnqueuedState.DefaultQueue &&
                ctx.ExpectedStates.SequenceEqual(new[] { ScheduledState.StateName }))));

            _connection.Verify(x => x.Dispose());
        }

        [Fact]
        public void Execute_MovesJobStateToEnqueued_InCustomQueue()
        {
            _connection.Setup(x => x.GetAllValuesWithScoresFromSetQueueWithinScoreRange(
                "schedule", "custom_queue", 0, It.Is<double>(time => time > 0))).Returns(_jobSet);
            var scheduler = CreateScheduler("custom_queue");

            scheduler.Execute(_context.Object);

            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == JobId &&
                ctx.NewState is EnqueuedState &&
                ((EnqueuedState)ctx.NewState).Queue == "custom_queue" &&
                ctx.ExpectedStates.SequenceEqual(new[] { ScheduledState.StateName }))));

            _connection.Verify(x => x.Dispose());
        }

        [Fact]
        public void Execute_DoesNotCallStateChanger_ForJobsNotInDesignatedQueues()
        {
            _connection.Setup(x => x.GetAllValuesWithScoresFromSetQueueWithinScoreRange(
                "schedule", "default", 0, It.Is<double>(time => time > 0))).Returns((Dictionary<string, double>) null);

            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _stateChanger.Verify(
                x => x.ChangeState(It.IsAny<StateChangeContext>()),
                Times.Never);
        }

        [Fact]
        public void Execute_DoesNotCallStateChanger_IfThereAreNoJobsToEnqueue()
        {
            _connection.Setup(x => x.GetAllValuesWithScoresFromSetQueueWithinScoreRange(
                "schedule", It.IsAny<string>(), 0, It.Is<double>(time => time > 0))).Returns((Dictionary<string, double>)null);

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

            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _transaction.Verify(x => x.RemoveFromSet("schedule", JobId));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void Execute_ActsWithinADistributedLock()
        {
            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _connection.Verify(x => x.AcquireDistributedLock(It.IsAny<string>(), It.IsAny<TimeSpan>()));
            _distributedLock.Verify(x => x.Dispose());
        }

        private DelayedJobScheduler CreateScheduler(params string[] additionalQueues)
        {
            var queues = new List<string> { EnqueuedState.DefaultQueue };
            queues.AddRange(additionalQueues);

            foreach (string queueName in queues)
            {
                _connection
                    .Setup(x => x.AcquireDistributedLock($"locks:schedulepoller:{ queueName }", It.IsAny<TimeSpan>()))
                    .Returns(_distributedLock.Object);
            }

            return new DelayedJobScheduler(Timeout.InfiniteTimeSpan, _stateChanger.Object, queues.ToArray());
        }
    }
}
