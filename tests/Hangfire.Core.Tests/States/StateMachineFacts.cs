using System;
using System.Collections.Generic;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class StateMachineFacts
    {
        private const string StateName = "State";
        private const string JobId = "1";
        private const string OldStateName = "Old";
        private static readonly string[] FromOldState = { OldStateName };

        private readonly Mock<IStorageConnection> _connection;
        private readonly Job _job;
        private readonly Dictionary<string, string> _parameters;
        private readonly Mock<IState> _state;
        private readonly Mock<IStateChangeProcess> _stateChangeProcess;
        private readonly Mock<IDisposable> _distributedLock;

        public StateMachineFacts()
        {
            _stateChangeProcess = new Mock<IStateChangeProcess>();

            _job = Job.FromExpression(() => Console.WriteLine());
            _parameters = new Dictionary<string, string>();
            _state = new Mock<IState>();
            _state.Setup(x => x.Name).Returns(StateName);

            _connection = new Mock<IStorageConnection>();

            _connection.Setup(x => x.CreateExpiredJob(
                It.IsAny<Job>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>())).Returns(JobId);

            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData
                {
                    State = OldStateName,
                    Job = _job
                });

            _distributedLock = new Mock<IDisposable>();
            _connection
                .Setup(x => x.AcquireDistributedLock(String.Format("job:{0}:state-lock", JobId), It.IsAny<TimeSpan>()))
                .Returns(_distributedLock.Object);

            _stateChangeProcess
                .Setup(x => x.ChangeState(
                    It.Is<StateContext>(s => s.JobId == JobId && s.Job == _job), 
                    _state.Object, 
                    OldStateName))
                .Returns(true);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateMachine(null, _stateChangeProcess.Object));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateChangeProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateMachine(_connection.Object, null));

            Assert.Equal("stateChangeProcess", exception.ParamName);
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

            _connection.Verify(x => x.CreateExpiredJob(
				job,
                It.Is<Dictionary<string, string>>(d => d["Name"] == "Value"),
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void CreateInState_ChangesTheStateOfACreatedJob()
        {
            var stateMachine = CreateStateMachine();

            stateMachine.CreateInState(_job, _parameters, _state.Object);

            _stateChangeProcess.Verify(x => x.ChangeState(
                It.Is<StateContext>(sc => sc.JobId == JobId && sc.Job == _job),
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
                () => stateMachine.TryToChangeState(null, _state.Object, FromOldState));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void TryToChangeState_ThrowsAnException_WhenToStateIsNull()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentNullException>(
                () => stateMachine.TryToChangeState(JobId, null, FromOldState));

            Assert.Equal("toState", exception.ParamName);
        }

        [Fact]
        public void TryToChangeState_ThrowsAnException_WhenFromStatesIsEmpty()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentException>(
                () => stateMachine.TryToChangeState(JobId, _state.Object, new string[0]));

            Assert.Equal("fromStates", exception.ParamName);
        }

        [Fact]
        public void TryToChangeState_WorksWithinAJobLock()
        {
            var stateMachine = CreateStateMachine();

            stateMachine.TryToChangeState(JobId, _state.Object, FromOldState);

            _distributedLock.Verify(x => x.Dispose());
        }

        [Fact]
        public void TryToChangeState_ChangesTheStateOfTheJob()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.TryToChangeState(JobId, _state.Object, FromOldState);

            // Assert
            _stateChangeProcess.Verify(x => x.ChangeState(
                It.Is<StateContext>(sc => sc.JobId == JobId && sc.Job.Type.Name.Equals("Console")),
                _state.Object,
                OldStateName));

            Assert.True(result);
        }

        [Fact]
        public void TryToChangeState_ChangesTheStateOfTheJob_WhenFromStatesIsNull()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act
            stateMachine.TryToChangeState(JobId, _state.Object, null);

            // Assert
            _stateChangeProcess.Verify(x => x.ChangeState(
                It.IsNotNull<StateContext>(),
                _state.Object,
                OldStateName));
        }

        [Fact]
        public void TryToChangeState_ReturnsFalse_WhenJobIsNotFound()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData(It.IsAny<string>()))
                .Returns((JobData)null);

            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.TryToChangeState(JobId, _state.Object, FromOldState);

            // Assert
            Assert.False(result);
            _connection.Verify(x => x.GetJobData(JobId));

            _stateChangeProcess.Verify(
                x => x.ChangeState(It.IsAny<StateContext>(), It.IsAny<IState>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void TryToChangeState_ReturnsFalse_WhenFromStatesArgumentDoesNotContainCurrentState()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.TryToChangeState(
                JobId, _state.Object, new [] { "AnotherState" });

            // Assert
            Assert.False(result);

            _stateChangeProcess.Verify(
                x => x.ChangeState(It.IsAny<StateContext>(), It.IsAny<IState>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void TryToChangeState_ReturnsFalse_WhenStateChangeReturnsFalse()
        {
            // Arrange
            _stateChangeProcess
                .Setup(x => x.ChangeState(It.IsAny<StateContext>(), It.IsAny<IState>(), It.IsAny<string>()))
                .Returns(false);

            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.TryToChangeState(JobId, _state.Object, FromOldState);

            // Assert
            _stateChangeProcess.Verify(
                x => x.ChangeState(It.IsAny<StateContext>(), It.IsAny<IState>(), It.IsAny<string>()));

            Assert.False(result);
        }

        [Fact]
        public void TryToChangeState_MoveJobToTheFailedState_IfMethodDataCouldNotBeResolved()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData
                {
                    State = OldStateName,
                    Job = null,
                    LoadException = new JobLoadException("asd", new InvalidOperationException())
                });

            _stateChangeProcess
                .Setup(x => x.ChangeState(It.IsAny<StateContext>(), It.IsAny<IState>(), It.IsAny<string>()))
                .Returns(true);

            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.TryToChangeState(JobId, _state.Object, FromOldState);

            // Assert
            _stateChangeProcess.Verify(x => x.ChangeState(
                It.Is<StateContext>(sc => sc.JobId == JobId && sc.Job == null),
                It.Is<FailedState>(s => s.Exception != null),
                OldStateName));

            Assert.False(result);
        }

        [Fact]
        public void TryToChangeState_MoveJobToTheGivenState_IfStateIgnoresThisException_AndMethodDataCouldNotBeResolved()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData
                {
                    State = OldStateName,
                    Job = null,
                    LoadException = new JobLoadException("asd", new Exception())
                });

            _stateChangeProcess
                .Setup(x => x.ChangeState(It.IsAny<StateContext>(), It.IsAny<IState>(), It.IsAny<string>()))
                .Returns(true);

            _state.Setup(x => x.IgnoreJobLoadException).Returns(true);

            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.TryToChangeState(JobId, _state.Object, FromOldState);

            // Assert
            _stateChangeProcess.Verify(x => x.ChangeState(
                It.IsAny<StateContext>(),
                _state.Object,
                OldStateName));

            Assert.True(result);
        }

        private StateMachine CreateStateMachine()
        {
            return new StateMachine(
                _connection.Object,
                _stateChangeProcess.Object);
        }
    }
}
