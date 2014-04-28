using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class StateMachineFacts
    {
        private readonly Mock<IStorageConnection> _connection
            = new Mock<IStorageConnection>();
        private readonly Mock<IWriteOnlyTransaction> _transaction
            = new Mock<IWriteOnlyTransaction>();
        private readonly List<IStateHandler> _handlers = new List<IStateHandler>();
        private readonly List<object> _filters = new List<object>();

        private readonly Job _job;
        private readonly Dictionary<string, string> _parameters;
        private readonly Mock<State> _state;
        private const string StateName = "State";
        private const string OldStateName = "Old";

        private const string JobId = "1";

        public StateMachineFacts()
        {
            _job = Job.FromExpression(() => Console.WriteLine("Hello"));
            _parameters = new Dictionary<string, string>();
            _state = new Mock<State>();
            _state.Setup(x => x.Name).Returns(StateName);

            _connection.Setup(x => x.CreateWriteTransaction())
                .Returns(_transaction.Object);
            _connection.Setup(x => x.CreateExpiredJob(
                It.IsAny<Job>(),
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
        public void Ctor_ThrowsAnException_WhenHandlersValueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateMachine(_connection.Object, null, _filters));

            Assert.Equal("handlers", exception.ParamName);
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
                It.Is<Job>(j => j.Type == typeof(Console) && j.Arguments[0] == "SomeString"),
                It.Is<Dictionary<string, string>>(d => d["Name"] == "Value"),
                It.IsAny<TimeSpan>()));
        }

        [Fact]
        public void CreateInState_ChangesTheStateOfACreatedJob()
        {
            var stateMachine = CreateStateMachineMock();

            stateMachine.Object.CreateInState(_job, _parameters, _state.Object);

            stateMachine.Verify(x => x.ChangeState(
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
            _connection.Setup(x => x.AcquireJobLock("1")).Returns(disposableMock.Object);

            var stateMachine = CreateStateMachine();

            stateMachine.TryToChangeState("1", _state.Object, new[] { "Old" });

            disposableMock.Verify(x => x.Dispose());
        }

        [Fact]
        public void TryToChangeState_ReturnsFalse_WhenJobIsNotFound()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData(It.IsAny<string>()))
                .Returns((JobData)null);

            var stateMachine = CreateStateMachineMock();

            // Act
            var result = stateMachine.Object.TryToChangeState("1", _state.Object, new [] { "Old" });

            // Assert
            Assert.False(result);
            _connection.Verify(x => x.GetJobData("1"));

            stateMachine.Verify(
                x => x.ChangeState(It.IsAny<StateContext>(), It.IsAny<State>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void TryToChangeState_ReturnsFalse_WhenFromStatesArgumentDoesNotContainCurrentState()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData("1"))
                .Returns(new JobData
                {
                    State = "Old",
                    Job = Job.FromExpression(() => Console.WriteLine())
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
        public void TryToChangeState_ReturnsFalse_WhenStateChangeReturnsFalse()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData("1"))
                .Returns(new JobData
                {
                    State = "Old",
                    Job = Job.FromExpression(() => Console.WriteLine())
                });

            var stateMachine = CreateStateMachineMock();
            stateMachine.Setup(x => x.ChangeState(It.IsAny<StateContext>(), It.IsAny<State>(), It.IsAny<string>()))
                .Returns(false);

            // Act
            var result = stateMachine.Object.TryToChangeState("1", _state.Object, new[] { "Old" });

            // Assert
            stateMachine.Verify(x => x.ChangeState(It.IsAny<StateContext>(), It.IsAny<State>(), It.IsAny<string>()));
            Assert.False(result);

        }

        [Fact]
        public void TryToChangeState_MoveJobToTheSpecifiedState_WhenMethodDataCouldBeFound()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData("1"))
                .Returns(new JobData
                {
                    State = "Old",
                    Job = Job.FromExpression(() => Console.WriteLine())
                });

            var stateMachine = CreateStateMachineMock();

            stateMachine.Setup(x => x.ChangeState(It.IsAny<StateContext>(), It.IsAny<State>(), It.IsAny<string>()))
                .Returns(true);

            // Act
            var result = stateMachine.Object
                .TryToChangeState("1", _state.Object, new[] { "Old" });

            // Assert
            stateMachine.Verify(x => x.ChangeState(
                It.Is<StateContext>(sc => sc.JobId == "1" && sc.Job.Type.Name.Equals("Console")),
                _state.Object,
                "Old"));

            Assert.True(result);
        }

        [Fact]
        public void TryToChangeState_MoveJobToTheFailedState_IfMethodDataCouldNotBeResolved()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData("1"))
                .Returns(new JobData
                {
                    State = "Old",
                    Job = null,
                    LoadException = new JobLoadException()
                });

            var stateMachine = CreateStateMachineMock();

            // Act
            var result = stateMachine.Object.TryToChangeState("1", _state.Object, new[] { "Old" });

            // Assert
            stateMachine.Verify(x => x.ChangeState(
                It.Is<StateContext>(sc => sc.JobId == "1" && sc.Job == null),
                It.Is<FailedState>(s => s.Exception != null),
                "Old"));

            Assert.False(result);
        }

        [Fact]
        public void ChangeState_AppliesState_AndReturnsTrue()
        {
            var stateMachine = CreateStateMachineMock();
            var context = new StateContext("1", Job.FromExpression(() => Console.WriteLine()));
            
            var result = stateMachine.Object.ChangeState(
                context, _state.Object, OldStateName);

            stateMachine.Verify(x => x.ApplyState(
                context, _state.Object, OldStateName, It.IsNotNull<IEnumerable<IApplyStateFilter>>()));
            Assert.True(result);
        }

        [Fact]
        public void ChangeState_AppliesOnlyElectedState()
        {
            var stateMachine = CreateStateMachineMock();
            var context = new StateContext("1", Job.FromExpression(() => Console.WriteLine()));
            var electedState = new Mock<State>();

            stateMachine
                .Setup(x => x.ElectState(
                    context, _state.Object, OldStateName, It.IsNotNull<IEnumerable<IElectStateFilter>>()))
                .Returns(electedState.Object);

            stateMachine.Object.ChangeState(context, _state.Object, OldStateName);

            stateMachine.Verify(x => x.ApplyState(
                context, electedState.Object, OldStateName, It.IsAny<IEnumerable<IApplyStateFilter>>()));
        }

        [Fact]
        public void ChangeState_AppliesFailedState_WhenThereIsAnException()
        {
            var stateMachine = CreateStateMachineMock();
            var context = new StateContext("1", Job.FromExpression(() => Console.WriteLine()));
            var exception = new NotSupportedException();

            stateMachine.Setup(x => x.ApplyState(
                context, _state.Object, OldStateName, It.IsAny<IEnumerable<IApplyStateFilter>>()))
                .Throws(exception);

            var result = stateMachine.Object.ChangeState(
                context, _state.Object, OldStateName);

            stateMachine.Verify(x => x.ApplyState(
                context, 
                It.Is<FailedState>(s => s.Exception == exception), 
                OldStateName,
                It.Is<IEnumerable<IApplyStateFilter>>(f => !f.Any())));
            Assert.False(result);
        }

        [Fact]
        public void ApplyState_RunsAllHandlers()
        {
            // Arrange
            var handler1 = new Mock<IStateHandler>();
            handler1.Setup(x => x.StateName).Returns(StateName);

            var handler2 = new Mock<IStateHandler>();
            handler2.Setup(x => x.StateName).Returns(StateName);

            _handlers.Add(handler1.Object);
            _handlers.Add(handler2.Object);

            var stateMachine = CreateStateMachine();
            var context = new StateContext("1", Job.FromExpression(() => Console.WriteLine()));

            // Act
            stateMachine.ApplyState(
                context, _state.Object, OldStateName, Enumerable.Empty<IApplyStateFilter>());

            // Assert
            handler1.Verify(x => x.Apply(
                It.Is<ApplyStateContext>(c => 
                    c.JobId == context.JobId 
                    && c.Job == context.Job 
                    && c.NewState == _state.Object 
                    && c.OldStateName == OldStateName),
                It.IsAny<IWriteOnlyTransaction>()));
        }

        private StateMachine CreateStateMachine()
        {
            return new StateMachine(
                _connection.Object,
                _handlers,
                _filters);
        }

        private Mock<StateMachine> CreateStateMachineMock()
        {
            return new Mock<StateMachine>(
                _connection.Object,
                _handlers,
                _filters)
            {
                CallBase = true
            };
        }
    }
}
