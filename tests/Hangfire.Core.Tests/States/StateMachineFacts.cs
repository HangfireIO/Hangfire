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
        private readonly Mock<IStateChangeProcess> _process;
        private readonly Mock<IDisposable> _distributedLock;
        private readonly Mock<IWriteOnlyTransaction> _transaction; 

        public StateMachineFacts()
        {
            _process = new Mock<IStateChangeProcess>();

            _job = Job.FromExpression(() => Console.WriteLine());
            _parameters = new Dictionary<string, string>();
            _state = new Mock<IState>();
            _state.Setup(x => x.Name).Returns(StateName);

            _connection = new Mock<IStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();

            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

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
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateMachine(null, _process.Object));

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
        public void Process_ReturnsTheGiven_StateChangingProcess()
        {
            var stateMachine = CreateStateMachine();

            var result = stateMachine.Process;

            Assert.Same(_process.Object, result);
        }

        [Fact]
        public void CreateJob_ThrowsAnException_WhenJobIsNull()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentNullException>(
                () => stateMachine.CreateJob(null, _parameters, _state.Object));

            Assert.Equal("job", exception.ParamName);
        }

        [Fact]
        public void CreateJob_ThrowsAnException_WhenParametersIsNull()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentNullException>(
                () => stateMachine.CreateJob(_job, null, _state.Object));

            Assert.Equal("parameters", exception.ParamName);
        }

        [Fact]
        public void CreateJob_ThrowsAnException_WhenStateIsNull()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentNullException> (
                () => stateMachine.CreateJob(_job, _parameters, null));

            Assert.Equal("state", exception.ParamName);
        }

        [Fact]
        public void CreateJob_CreatesExpiredJob()
        {
            var job = Job.FromExpression(() => Console.WriteLine("SomeString"));
            _parameters.Add("Name", "Value");

            var stateMachine = CreateStateMachine();

            stateMachine.CreateJob(job, _parameters, _state.Object);

            _connection.Verify(x => x.CreateExpiredJob(
				job,
                It.Is<Dictionary<string, string>>(d => d["Name"] == "Value"),
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void CreateJob_ChangesTheStateOfACreatedJob()
        {
            var stateMachine = CreateStateMachine();

            stateMachine.CreateJob(_job, _parameters, _state.Object);

            _process.Verify(x => x.ApplyState(
                _transaction.Object,
                It.Is<ApplyStateContext>(sc => sc.JobId == JobId && sc.Job == _job
                    && sc.NewState == _state.Object && sc.OldStateName == null),
                true));
        }

        [Fact]
        public void CreateJob_ReturnsNewJobId()
        {
            var stateMachine = CreateStateMachine();
            Assert.Equal(JobId, stateMachine.CreateJob(_job, _parameters, _state.Object));
        }

        [Fact]
        public void ChangeState_ThrowsAnException_WhenJobIdIsNull()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentNullException>(
                () => stateMachine.ChangeState(null, _state.Object, FromOldState));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void ChangeState_ThrowsAnException_WhenToStateIsNull()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentNullException>(
                () => stateMachine.ChangeState(JobId, null, FromOldState));

            Assert.Equal("toState", exception.ParamName);
        }

        [Fact]
        public void ChangeState_ThrowsAnException_WhenFromStatesIsEmpty()
        {
            var stateMachine = CreateStateMachine();

            var exception = Assert.Throws<ArgumentException>(
                () => stateMachine.ChangeState(JobId, _state.Object, new string[0]));

            Assert.Equal("fromStates", exception.ParamName);
        }

        [Fact]
        public void ChangeState_WorksWithinAJobLock()
        {
            var stateMachine = CreateStateMachine();

            stateMachine.ChangeState(JobId, _state.Object, FromOldState);

            _distributedLock.Verify(x => x.Dispose());
        }

        [Fact]
        public void TryToChangeState_ChangesTheStateOfTheJob()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.ChangeState(JobId, _state.Object, FromOldState);

            // Assert
            _process.Verify(x => x.ApplyState(
                _transaction.Object,
                It.Is<ApplyStateContext>(sc => sc.JobId == JobId && sc.Job.Type.Name.Equals("Console")
                    && sc.NewState == _state.Object && sc.OldStateName == OldStateName),
                true));

            Assert.True(result);
        }

        [Fact]
        public void ChangeState_ChangesTheStateOfTheJob_WhenFromStatesIsNull()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act
            stateMachine.ChangeState(JobId, _state.Object, null);

            // Assert
            _process.Verify(x => x.ApplyState(
                _transaction.Object,
                It.Is<ApplyStateContext>(ctx => ctx.NewState == _state.Object && ctx.OldStateName == OldStateName),
                true));
        }

        [Fact]
        public void ChangeState_ReturnsFalse_WhenJobIsNotFound()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData(It.IsAny<string>()))
                .Returns((JobData)null);

            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.ChangeState(JobId, _state.Object, FromOldState);

            // Assert
            Assert.False(result);
            _connection.Verify(x => x.GetJobData(JobId));

            _process.Verify(
                x => x.ApplyState(It.IsAny<IWriteOnlyTransaction>(), It.IsAny<ApplyStateContext>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public void ChangeState_ReturnsFalse_WhenFromStatesArgumentDoesNotContainCurrentState()
        {
            // Arrange
            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.ChangeState(
                JobId, _state.Object, new [] { "AnotherState" });

            // Assert
            Assert.False(result);

            _process.Verify(
                x => x.ApplyState(It.IsAny<IWriteOnlyTransaction>(), It.IsAny<ApplyStateContext>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public void ChangeState_ReturnsFalse_WhenApplyStateThrowsException()
        {
            // Arrange
            _process.Setup(x => x.ApplyState(It.IsAny<IWriteOnlyTransaction>(), It.IsAny<ApplyStateContext>(), true))
                .Throws(new Exception());

            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.ChangeState(JobId, _state.Object, FromOldState);

            // Assert
            _process.Verify(x => x.ApplyState(_transaction.Object, It.IsNotNull<ApplyStateContext>(), false));

            Assert.False(result);
        }

        [Fact]
        public void ChangeState_MoveJobToTheFailedState_IfMethodDataCouldNotBeResolved()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData
                {
                    State = OldStateName,
                    Job = null,
                    LoadException = new JobLoadException("asd", new InvalidOperationException())
                });

            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.ChangeState(JobId, _state.Object, FromOldState);

            // Assert
            _process.Verify(x => x.ApplyState(
                _transaction.Object,
                It.Is<ApplyStateContext>(ctx => ctx.JobId == JobId && ctx.Job == null && ctx.NewState is FailedState),
                true));

            Assert.False(result);
        }

        [Fact]
        public void ChangeState_MoveJobToTheGivenState_IfStateIgnoresThisException_AndMethodDataCouldNotBeResolved()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData(JobId))
                .Returns(new JobData
                {
                    State = OldStateName,
                    Job = null,
                    LoadException = new JobLoadException("asd", new Exception())
                });

            _state.Setup(x => x.IgnoreJobLoadException).Returns(true);

            var stateMachine = CreateStateMachine();

            // Act
            var result = stateMachine.ChangeState(JobId, _state.Object, FromOldState);

            // Assert
            _process.Verify(x => x.ApplyState(
                _transaction.Object, 
                It.Is<ApplyStateContext>(ctx => ctx.NewState == _state.Object),
                true));

            Assert.True(result);
        }

        [Fact]
        public void ChangeState_CommitsTheNewState_AndReturnsTrue()
        {
            // Arrange
            var machine = CreateStateMachine();

            // Act
            var result = machine.ChangeState(JobId, _state.Object, new[] { OldStateName });

            // Assert
            _process.Verify(x => x.ApplyState(
                _transaction.Object,
                It.Is<ApplyStateContext>(ctx => ctx.NewState == _state.Object && ctx.OldStateName == OldStateName
                    && ctx.Job == _job && ctx.JobId == JobId),
                true));

            _transaction.Verify(x => x.Commit());

            Assert.True(result);
        }

        [Fact]
        public void ChangeState_SetsAnotherState_WhenItWasElected()
        {
            // Arrange
            var anotherState = new Mock<IState>();

            _process.Setup(x => x.ElectState(_connection.Object, It.IsAny<ElectStateContext>()))
                .Callback((IStorageConnection connection, ElectStateContext context) => context.CandidateState = anotherState.Object);

            var machine = CreateStateMachine();

            // Act
            machine.ChangeState(JobId, _state.Object, new[] { OldStateName });

            // Assert - Sequence
            _process.Verify(x => x.ApplyState(
                _transaction.Object, 
                It.Is<ApplyStateContext>(ctx => ctx.NewState == anotherState.Object),
                true));
        }

        [Fact]
        public void ChangeState_AppliesFailedState_WhenThereIsAnException_AndReturnsFalse()
        {
            // Arrange
            var exception = new NotSupportedException();

            _process.Setup(
                x => x.ApplyState(It.IsAny<IWriteOnlyTransaction>(), It.IsAny<ApplyStateContext>(), true))
                .Throws(exception);

            var machine = CreateStateMachine();

            // Act
            var result = machine.ChangeState(JobId, _state.Object, new[] { OldStateName });

            // Assert
            _process.Verify(x => x.ApplyState(
                _transaction.Object,
                It.Is<ApplyStateContext>(ctx => ctx.NewState is FailedState && ((FailedState)ctx.NewState).Exception == exception),
                false));

            Assert.False(result);
        }

        private StateMachine CreateStateMachine()
        {
            return new StateMachine(
                _connection.Object,
                _process.Object);
        }
    }
}
