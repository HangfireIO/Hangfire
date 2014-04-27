using System;
using System.Threading;
using HangFire.Common.States;
using HangFire.Server.Components;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class SchedulePollerFacts
    {
        private const string JobId = "id";
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly Mock<IStateMachineFactory> _stateMachineFactory;
        private readonly CancellationToken _token;

        public SchedulePollerFacts()
        {
            _storage = new Mock<JobStorage>();
            _connection = new Mock<IStorageConnection>();
            _stateMachine = new Mock<IStateMachine>();
            _token = new CancellationToken(true);

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
                () => new SchedulePoller2(
                    null, _stateMachineFactory.Object, TimeSpan.FromMilliseconds(-1)));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SchedulePoller2(
                    _storage.Object, null, TimeSpan.FromMilliseconds(-1)));

            Assert.Equal("stateMachineFactory", exception.ParamName);
        }

        [Fact]
        public void Execute_TakesConnectionAndDisposesIt()
        {
            var scheduler = CreateScheduler();

            scheduler.Execute(_token);

            _storage.Verify(x => x.GetConnection());
            _connection.Verify(x => x.Dispose());
        }

        [Fact]
        public void Execute_MovesJobStateToEnqueued()
        {
            var scheduler = CreateScheduler();

            scheduler.Execute(_token);

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

            scheduler.Execute(_token);

            _stateMachine.Verify(
                x => x.TryToChangeState(It.IsAny<string>(), It.IsAny<State>(), It.IsAny<string[]>()),
                Times.Never);
        }

        private SchedulePoller2 CreateScheduler()
        {
            return new SchedulePoller2(_storage.Object, _stateMachineFactory.Object, TimeSpan.Zero);
        }
    }
}
