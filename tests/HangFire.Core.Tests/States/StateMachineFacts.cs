using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class StateMachineFacts
    {
        private readonly Mock<IStorageConnection> _connectionMock =
            new Mock<IStorageConnection>();
        private readonly List<StateHandler> _handlers = new List<StateHandler>();
        private readonly List<object> _filters = new List<object>();

        private readonly Job _job;
        private readonly Dictionary<string, string> _parameters;
        private readonly Mock<State> _state;

        private const string JobId = "1";
        private const string StateName = "State";

        public StateMachineFacts()
        {
            _job = Job.FromExpression(() => Console.WriteLine("Hello"));
            _parameters = new Dictionary<string, string>();
            _state = new Mock<State>();
            _state.Setup(x => x.Name).Returns(StateName);

            _connectionMock.Setup(x => x.CreateWriteTransaction())
                .Returns(new Mock<IWriteOnlyTransaction>().Object);
            _connectionMock.Setup(x => x.CreateExpiredJob(
                It.IsAny<InvocationData>(),
                It.IsAny<string[]>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<TimeSpan>())).Returns(JobId);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateMachine(null, _handlers, _filters));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void CreateInState_ThrowsAnException_WhenJobIsNull()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentNullException>(
                () => stateMachine.CreateInState(null, _parameters, _state.Object));

            Assert.Equal("job", exception.ParamName);
        }

        [Fact]
        public void CreateInState_ThrowsAnException_WhenParametersIsNull()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentNullException>(
                () => stateMachine.CreateInState(_job, null, _state.Object));

            Assert.Equal("parameters", exception.ParamName);
        }

        [Fact]
        public void CreateInState_ThrowsAnException_WhenStateIsNull()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentNullException> (
                () => stateMachine.CreateInState(_job, _parameters, null));

            Assert.Equal("state", exception.ParamName);
        }

        [Fact]
        public void CreateInState_CreatesExpiredJob()
        {
            var job = Job.FromExpression(() => Console.WriteLine("SomeString"));
            _parameters.Add("Name", "Value");

            var stateMachine = CreateStateMachine();

            stateMachine.CreateInState(job, _parameters, _state.Object);

            _connectionMock.Verify(x => x.CreateExpiredJob(
                job.MethodData.Serialize(),
                new [] { "SomeString" },
                It.Is<Dictionary<string, string>>(d => d["Name"] == "Value"),
                It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void CreateInState_ChangesTheStateOfACreatedJob()
        {
            var stateMachine = CreateStateMachineMock();

            stateMachine.Object.CreateInState(_job, _parameters, _state.Object);

            stateMachine.Verify(x => x.ChangeState(
                It.Is<StateContext>(sc => sc.JobId == JobId && sc.MethodData == _job.MethodData),
                _state.Object,
                null));
        }

        [Fact]
        public void CreateInState_ReturnsNewJobId()
        {
            var stateMachine = CreateStateMachine();
            Assert.Equal(JobId, stateMachine.CreateInState(_job, _parameters, _state.Object));
        }

        [Fact]
        public void TryToChangeState_ThrowsAnException_WhenJobIdIsNull()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentNullException>(
                () => stateMachine.TryToChangeState(null, _state.Object, new[] { "Old" }));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void TryToChangeState_ThrowsAnException_WhenToStateIsNull()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentNullException>(
                () => stateMachine.TryToChangeState("1", null, new[] { "Old" }));

            Assert.Equal("toState", exception.ParamName);
        }

        [Fact]
        public void TryToChangeState_ThrowsAnException_WhenFromStatesIsNull()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentNullException>(
                () => stateMachine.TryToChangeState("1", _state.Object, null));

            Assert.Equal("fromStates", exception.ParamName);
        }

        [Fact]
        public void TryToChangeState_WorksWithinAJobLock()
        {
            var disposableMock = new Mock<IDisposable>();
            _connectionMock.Setup(x => x.AcquireJobLock("1")).Returns(disposableMock.Object);

            var stateMachine = CreateStateMachine();

            stateMachine.TryToChangeState("1", _state.Object, new[] { "Old" });

            disposableMock.Verify(x => x.Dispose());
        }

        [Fact]
        public void TryToChangeState_ReturnsFalse_WhenJobIsNotFound()
        {
            // Arrange
            _connectionMock.Setup(x => x.GetJobStateAndInvocationData(It.IsAny<string>()))
                .Returns((StateAndInvocationData)null);

            var stateMachine = CreateStateMachineMock();

            // Act
            var result = stateMachine.Object.TryToChangeState("1", _state.Object, new [] { "Old" });

            // Assert
            Assert.False(result);
            _connectionMock.Verify(x => x.GetJobStateAndInvocationData("1"));

            stateMachine.Verify(
                x => x.ChangeState(It.IsAny<StateContext>(), It.IsAny<State>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void TryToChangeState_ReturnsFalse_WhenFromStatesArgumentDoesNotContainCurrentState()
        {
            // Arrange
            _connectionMock.Setup(x => x.GetJobStateAndInvocationData("1"))
                .Returns(new StateAndInvocationData
                {
                    State = "Old",
                    InvocationData = MethodData.FromExpression(() => Console.WriteLine()).Serialize()
                });

            var stateMachine = CreateStateMachineMock();

            // Act
            var result = stateMachine.Object
                .TryToChangeState("1", _state.Object, new [] { "AnotherState" });

            // Assert
            Assert.False(result);
            stateMachine.Verify(
                x => x.ChangeState(It.IsAny<StateContext>(), It.IsAny<State>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void TryToChangeState_MoveJobToTheSpecifiedState_WhenMethodDataCouldBeFound()
        {
            // Arrange
            _connectionMock.Setup(x => x.GetJobStateAndInvocationData("1"))
                .Returns(new StateAndInvocationData
                {
                    State = "Old",
                    InvocationData = MethodData.FromExpression(() => Console.WriteLine()).Serialize()
                });

            var stateMachine = CreateStateMachineMock();

            stateMachine.Setup(x => x.ChangeState(It.IsAny<StateContext>(), It.IsAny<State>(), It.IsAny<string>()))
                .Returns(true);

            // Act
            var result = stateMachine.Object
                .TryToChangeState("1", _state.Object, new[] { "Old" });

            // Assert
            stateMachine.Verify(x => x.ChangeState(
                It.Is<StateContext>(sc => sc.JobId == "1" && sc.MethodData.Type.Name.Equals("Console")),
                _state.Object,
                "Old"));

            Assert.True(result);
        }

        [Fact]
        public void TryToChangeState_MoveJobToTheFailedState_IfMethodDataCouldNotBeResolved()
        {
            // Arrange
            _connectionMock.Setup(x => x.GetJobStateAndInvocationData("1"))
                .Returns(new StateAndInvocationData
                {
                    State = "Old",
                    InvocationData = new InvocationData("NotExists", "NotExists", null)
                });

            var stateMachine = CreateStateMachineMock();

            // Act
            stateMachine.Object.TryToChangeState("1", _state.Object, new[] { "Old" });

            // Assert
            stateMachine.Verify(x => x.ChangeState(
                It.Is<StateContext>(sc => sc.JobId == "1" && sc.MethodData == null),
                It.Is<FailedState>(s => s.Exception != null),
                "Old"));
        }

        private StateMachine CreateStateMachine()
        {
            return new StateMachine(
                _connectionMock.Object,
                _handlers,
                _filters);
        }

        private Mock<StateMachine> CreateStateMachineMock()
        {
            return new Mock<StateMachine>(
                _connectionMock.Object,
                _handlers,
                _filters)
            {
                CallBase = false
            };
        }
    }
}
