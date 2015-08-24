using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class StateChangeProcessFacts
    {
        private const string StateName = "State";
        private const string JobId = "1";
        private const string OldStateName = "Old";
        private static readonly string[] FromOldState = { OldStateName };

        private readonly Mock<IStorageConnection> _connection;
        private readonly Job _job;
        private readonly Mock<IState> _state;
        private readonly Mock<IStateMachine> _process;
        private readonly Mock<IDisposable> _distributedLock;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly CancellationTokenSource _cts;
        private readonly StateChangeContextMock _context;

        public StateChangeProcessFacts()
        {
            _process = new Mock<IStateMachine>();

            _job = Job.FromExpression(() => Console.WriteLine());
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

            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            _context = new StateChangeContextMock
            {
                BackgroundJobId = JobId,
                Connection = _connection,
                CancellationToken = _cts.Token,
                NewState = _state,
                ExpectedStates = FromOldState
            };
        }
        
        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateChangeProcess(null));

            Assert.Equal("stateMachine", exception.ParamName);
        }
        
        [Fact]
        public void ChangeState_WorksWithinAJobLock()
        {
            var process = CreateProcess();

            process.ChangeState(_context.Object);

            _distributedLock.Verify(x => x.Dispose());
        }

        [Fact]
        public void TryToChangeState_ChangesTheStateOfTheJob()
        {
            // Arrange
            var process = CreateProcess();

            // Act
            var result = process.ChangeState(_context.Object);

            // Assert
            _process.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(sc => sc.BackgroundJob.Id == JobId && sc.BackgroundJob.Job.Type.Name.Equals("Console")
                    && sc.NewState == _state.Object && sc.OldStateName == OldStateName)));

            Assert.NotNull(result);
            Assert.Equal(_state.Object.Name, result.Name);
        }

        [Fact]
        public void ChangeState_ChangesTheStateOfTheJob_WhenFromStatesIsNull()
        {
            // Arrange
            var process = CreateProcess();
            _context.ExpectedStates = null;

            // Act
            process.ChangeState(_context.Object);

            // Assert
            _process.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(ctx => ctx.NewState == _state.Object && ctx.OldStateName == OldStateName)));
        }

        [Fact]
        public void ChangeState_ReturnsNull_WhenJobIsNotFound()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData(It.IsAny<string>()))
                .Returns((JobData)null);

            var process = CreateProcess();

            // Act
            var result = process.ChangeState(_context.Object);

            // Assert
            Assert.Null(result);
            _connection.Verify(x => x.GetJobData(JobId));

            _process.Verify(
                x => x.ApplyState(It.IsAny<ApplyStateContext>()),
                Times.Never);
        }

        [Fact]
        public void ChangeState_DoesNotDoAnything_WhenStateIsNull_AndCancellationTokenIsCancelled()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData(It.IsAny<string>())).Returns(new JobData
            {
                Job = _job,
                State = null
            });

            var process = CreateProcess();
            _cts.Cancel();

            // Act
            var result = process.ChangeState(_context.Object);

            // Assert
            Assert.Null(result);

            _process.Verify(
                x => x.ElectState(It.IsAny<ElectStateContext>()),
                Times.Never);

            _process.Verify(
                x => x.ApplyState(It.IsAny<ApplyStateContext>()),
                Times.Never);
        }

        [Fact]
        public void ChangeState_WaitsFor_NonNullStateValue()
        {
            // Arrange
            var results = new Queue<JobData>();
            results.Enqueue(new JobData { Job = _job, State = null });
            results.Enqueue(new JobData { Job = _job, State = null });
            results.Enqueue(new JobData { Job = _job, State = OldStateName });

            _connection.Setup(x => x.GetJobData(It.IsAny<string>()))
                .Returns(results.Dequeue);

            var process = CreateProcess();

            // Act
            var result = process.ChangeState(_context.Object);

            // Assert
            Assert.Equal(0, results.Count);
            Assert.NotNull(result);
            Assert.Equal(_state.Object.Name, result.Name);
        }

        [Fact]
        public void ChangeState_ReturnsNull_WhenFromStatesArgumentDoesNotContainCurrentState()
        {
            // Arrange
            var process = CreateProcess();
            _context.ExpectedStates = new[] { "AnotherState" };

            // Act
            var result = process.ChangeState(_context.Object);

            // Assert
            Assert.Null(result);

            _process.Verify(
                x => x.ApplyState(It.IsAny<ApplyStateContext>()),
                Times.Never);
        }

        [Fact]
        public void ChangeState_ThrowsAnException_WhenApplyStateThrowsException()
        {
            // Arrange
            _process.Setup(x => x.ApplyState(It.IsAny<ApplyStateContext>()))
                .Throws(new FieldAccessException());

            var process = CreateProcess();

            // Act & Assert
            Assert.Throws<FieldAccessException>(
                () => process.ChangeState(_context.Object));
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

            var process = CreateProcess();

            // Act
            var result = process.ChangeState(_context.Object);

            // Assert
            _process.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(ctx => ctx.BackgroundJob.Id == JobId && 
                ctx.BackgroundJob.Job == null && ctx.NewState is FailedState)));

            Assert.IsType<FailedState>(result);
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

            var process = CreateProcess();

            // Act
            var result = process.ChangeState(_context.Object);

            // Assert
            _process.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(ctx => ctx.NewState == _state.Object)));

            Assert.NotNull(result);
            Assert.Equal(_state.Object.Name, result.Name);
        }

        [Fact]
        public void ChangeState_CommitsTheNewState_AndReturnsAppliedState()
        {
            // Arrange
            var machine = CreateProcess();
            _context.ExpectedStates = new[] { OldStateName };

            // Act
            var result = machine.ChangeState(_context.Object);

            // Assert
            _process.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(ctx => ctx.NewState == _state.Object && ctx.OldStateName == OldStateName
                    && ctx.BackgroundJob.Job == _job && ctx.BackgroundJob.Id == JobId)));

            _transaction.Verify(x => x.Commit());

            Assert.NotNull(result);
            Assert.Equal(_state.Object.Name, result.Name);
        }

        [Fact]
        public void ChangeState_SetsAnotherState_WhenItWasElected()
        {
            // Arrange
            var anotherState = new Mock<IState>();

            _process.Setup(x => x.ElectState(It.IsAny<ElectStateContext>()))
                .Callback((ElectStateContext context) => context.CandidateState = anotherState.Object);
            _context.ExpectedStates = new[] { OldStateName };

            var machine = CreateProcess();

            // Act
            machine.ChangeState(_context.Object);

            // Assert - Sequence
            _process.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(ctx => ctx.NewState == anotherState.Object)));
        }

        private StateChangeProcess CreateProcess()
        {
            return new StateChangeProcess(_process.Object);
        }
    }
}
