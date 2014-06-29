using System;
using System.Threading;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class SchedulePollerFacts
    {
        private const string JobId = "id";
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly Mock<IStateMachineFactory> _stateMachineFactory;
		private readonly CancellationTokenSource _cts;

        public SchedulePollerFacts()
        {
            _storage = new Mock<JobStorage>();
            _connection = new Mock<IStorageConnection>();
            _stateMachine = new Mock<IStateMachine>();
			_cts = new CancellationTokenSource();
			_cts.Cancel();

            _stateMachineFactory = new Mock<IStateMachineFactory>();
            _stateMachineFactory.Setup(x => x.Create(It.IsNotNull<IStorageConnection>()))
                .Returns(_stateMachine.Object);

            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
            _connection.Setup(x => x.GetFirstByLowestScoreFromSet(
                "schedule", 0, It.Is<double>(time => time > 0))).Returns(JobId);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SchedulePoller(
                    null, _stateMachineFactory.Object, Timeout.InfiniteTimeSpan));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SchedulePoller(
                    _storage.Object, null, Timeout.InfiniteTimeSpan));

            Assert.Equal("stateMachineFactory", exception.ParamName);
        }

        [Fact]
        public void Execute_TakesConnectionAndDisposesIt()
        {
            var scheduler = CreateScheduler();

			scheduler.Execute(_cts.Token);

            _storage.Verify(x => x.GetConnection());
            _connection.Verify(x => x.Dispose());
        }

        [Fact]
        public void Execute_MovesJobStateToEnqueued()
        {
            var scheduler = CreateScheduler();

			scheduler.Execute(_cts.Token);

            _stateMachine.Verify(x => x.TryToChangeState(
                JobId,
                It.IsAny<EnqueuedState>(),
                new[] { ScheduledState.StateName }));
        }

        [Fact]
        public void Execute_DoesNotCallStateMachine_IfThereAreNoJobsToEnqueue()
        {
            _connection.Setup(x => x.GetFirstByLowestScoreFromSet(
                "schedule", 0, It.Is<double>(time => time > 0))).Returns((string)null);
            var scheduler = CreateScheduler();

			scheduler.Execute(_cts.Token);

            _stateMachine.Verify(
                x => x.TryToChangeState(It.IsAny<string>(), It.IsAny<IState>(), It.IsAny<string[]>()),
                Times.Never);
        }

        private SchedulePoller CreateScheduler()
        {
            return new SchedulePoller(_storage.Object, _stateMachineFactory.Object, Timeout.InfiniteTimeSpan);
        }
    }
}
