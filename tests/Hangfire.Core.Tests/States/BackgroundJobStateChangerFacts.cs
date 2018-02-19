using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.States
{
    public class BackgroundJobStateChangerFacts
    {
        private const string StateName = "State";
        private const string JobId = "1";
        private const string OldStateName = "Old";
        private static readonly string[] FromOldState = { OldStateName };

        private readonly Mock<IStorageConnection> _connection;
        private readonly Job _job;
        private readonly Mock<IState> _state;
        private readonly Mock<IJobFilterProvider> _filterProvider;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly Mock<IDisposable> _distributedLock;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly CancellationTokenSource _cts;
        private readonly StateChangeContextMock _context;

        public BackgroundJobStateChangerFacts()
        {
            _stateMachine = new Mock<IStateMachine>();
            _filterProvider = new Mock<IJobFilterProvider>();
            _filterProvider.Setup(x => x.GetFilters(It.IsAny<Job>())).Returns(Enumerable.Empty<JobFilter>());

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
                .Setup(x => x.AcquireDistributedLock($"job:{JobId}:state-lock", It.IsAny<TimeSpan>()))
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

            _stateMachine.Setup(x => x.ApplyState(It.IsNotNull<ApplyStateContext>()))
                .Returns(_context.NewState.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenFilterProviderIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobStateChanger(null));

            Assert.Equal("filterProvider", exception.ParamName);
        }
        
        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobStateChanger(_filterProvider.Object, null));

            Assert.Equal("stateMachine", exception.ParamName);
        }
        
        [Fact]
        public void ChangeState_WorksWithinAJobLock()
        {
            var stateChanger = CreateStateChanger();

            stateChanger.ChangeState(_context.Object);

            _distributedLock.Verify(x => x.Dispose());
        }

        [Fact]
        public void TryToChangeState_ChangesTheStateOfTheJob()
        {
            // Arrange
            var stateChanger = CreateStateChanger();

            // Act
            var result = stateChanger.ChangeState(_context.Object);

            // Assert
            _stateMachine.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(sc => sc.BackgroundJob.Id == JobId && sc.BackgroundJob.Job.Type.Name.Equals("Console")
                    && sc.NewState == _state.Object && sc.OldStateName == OldStateName)));

            Assert.NotNull(result);
            Assert.Equal(_state.Object.Name, result.Name);
        }

        [Fact]
        public void ChangeState_ChangesTheStateOfTheJob_WhenFromStatesIsNull()
        {
            // Arrange
            var stateChanger = CreateStateChanger();
            _context.ExpectedStates = null;

            // Act
            stateChanger.ChangeState(_context.Object);

            // Assert
            _stateMachine.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(ctx => ctx.NewState == _state.Object && ctx.OldStateName == OldStateName)));
        }

        [Fact]
        public void ChangeState_ReturnsNull_WhenJobIsNotFound()
        {
            // Arrange
            _connection.Setup(x => x.GetJobData(It.IsAny<string>()))
                .Returns((JobData)null);

            var stateChanger = CreateStateChanger();

            // Act
            var result = stateChanger.ChangeState(_context.Object);

            // Assert
            Assert.Null(result);
            _connection.Verify(x => x.GetJobData(JobId));

            _stateMachine.Verify(
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

            var stateChanger = CreateStateChanger();
            _cts.Cancel();

            // Act
            var result = stateChanger.ChangeState(_context.Object);

            // Assert
            Assert.Null(result);

            _stateMachine.Verify(
                x => x.ApplyState(It.IsAny<ApplyStateContext>()),
                Times.Never);
        }

        [Fact]
        public void ChangeState_WaitsFor_NonNullJobDataAndStateValue()
        {
            // Arrange
            var results = new Queue<JobData>();
            results.Enqueue(null);
            results.Enqueue(new JobData { Job = _job, State = null });
            results.Enqueue(new JobData { Job = _job, State = OldStateName });

            _connection.Setup(x => x.GetJobData(It.IsAny<string>()))
                .Returns(results.Dequeue);

            var stateChanger = CreateStateChanger();

            // Act
            var result = stateChanger.ChangeState(_context.Object);

            // Assert
            Assert.Equal(0, results.Count);
            Assert.NotNull(result);
            Assert.Equal(_state.Object.Name, result.Name);
        }

        [Fact]
        public void ChangeState_ReturnsNull_WhenFromStatesArgumentDoesNotContainCurrentState()
        {
            // Arrange
            var stateChanger = CreateStateChanger();
            _context.ExpectedStates = new[] { "AnotherState" };

            // Act
            var result = stateChanger.ChangeState(_context.Object);

            // Assert
            Assert.Null(result);

            _stateMachine.Verify(
                x => x.ApplyState(It.IsAny<ApplyStateContext>()),
                Times.Never);
        }

        [Fact]
        public void ChangeState_ThrowsAnException_WhenApplyStateThrowsException()
        {
            // Arrange
            _stateMachine.Setup(x => x.ApplyState(It.IsAny<ApplyStateContext>()))
                .Throws(new FieldAccessException());

            var stateChanger = CreateStateChanger();

            // Act & Assert
            Assert.Throws<FieldAccessException>(
                () => stateChanger.ChangeState(_context.Object));
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

            var stateChanger = CreateStateChanger();

            // Act
            stateChanger.ChangeState(_context.Object);

            // Assert
            _stateMachine.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(ctx => ctx.BackgroundJob.Id == JobId && 
                ctx.BackgroundJob.Job == null && ctx.NewState is FailedState)));
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

            var stateChanger = CreateStateChanger();

            // Act
            var result = stateChanger.ChangeState(_context.Object);

            // Assert
            _stateMachine.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(ctx => ctx.NewState == _state.Object)));

            Assert.NotNull(result);
            Assert.Equal(_state.Object.Name, result.Name);
        }

        [Fact]
        public void ChangeState_CommitsTheNewState_AndReturnsAppliedState()
        {
            // Arrange
            var stateChanger = CreateStateChanger();
            _context.ExpectedStates = new[] { OldStateName };

            // Act
            var result = stateChanger.ChangeState(_context.Object);

            // Assert
            _stateMachine.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(ctx => ctx.NewState == _state.Object && ctx.OldStateName == OldStateName
                    && ctx.BackgroundJob.Job == _job && ctx.BackgroundJob.Id == JobId)));

            _transaction.Verify(x => x.Commit());

            Assert.NotNull(result);
            Assert.Equal(_state.Object.Name, result.Name);
        }

        [Fact]
        public void ChangeState_ReturnsState_ReturnedByAStateMachine()
        {
            // Arrange
            var anotherState = new Mock<IState>();

            _stateMachine.Setup(x => x.ApplyState(It.IsNotNull<ApplyStateContext>()))
                .Returns(anotherState.Object);

            var stateChanger = CreateStateChanger();

            // Act
            var result = stateChanger.ChangeState(_context.Object);

            // Assert
            Assert.Same(result, anotherState.Object);
        }

        [Fact]
        public void ChangeState_MovesJobToFailedState_AfterSomeRetryAttempts_WhenThereIsAnException()
        {
            // Arrange
            _stateMachine
                .Setup(x => x.ApplyState(It.Is<ApplyStateContext>(context => context.NewState == _state.Object)))
                .Throws<Exception>();

            var stateChanger = CreateStateChanger();

            // Act
            var result = stateChanger.ChangeState(_context.Object);

            // Assert
            Assert.IsType<FailedState>(result);

            _transaction.Verify(x => x.Commit(), Times.Once);

            _stateMachine.Verify(
                x => x.ApplyState(It.Is<ApplyStateContext>(context => context.NewState == result)), 
                Times.Once);

            _stateMachine.Verify(
                x => x.ApplyState(It.Is<ApplyStateContext>(context => context.NewState == _state.Object)),
                Times.AtLeast(2));
        }

        private BackgroundJobStateChanger CreateStateChanger()
        {
            return new BackgroundJobStateChanger(_filterProvider.Object, _stateMachine.Object);
        }
    }
}
