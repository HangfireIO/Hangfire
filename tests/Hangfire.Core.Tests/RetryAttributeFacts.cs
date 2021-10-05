using System;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class RetryAttributeFacts
    {
        private const string JobId = "id";

        private readonly FailedState _failedState;
        private readonly Mock<IStorageConnection> _connection;
        private readonly ElectStateContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        public RetryAttributeFacts()
        {
            _failedState = new FailedState(new InvalidOperationException());
            _connection = new Mock<IStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();

            _context = new ElectStateContextMock();
            _context.ApplyContext.BackgroundJob.Id = JobId;
            _context.ApplyContext.Connection = _connection;
            _context.ApplyContext.NewStateObject = _failedState;
        }

        [Fact]
        public void Ctor_SetsPositiveRetryAttemptsNumber_ByDefault()
        {
            var filter = new AutomaticRetryAttribute();
            Assert.Equal(10, filter.Attempts);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenAttemptsValueIsNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new AutomaticRetryAttribute { Attempts = -1 });
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenDelaysInSecondsIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AutomaticRetryAttribute { DelaysInSeconds = null });
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenDelaysInSecondsIsEmpty()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AutomaticRetryAttribute { DelaysInSeconds = new int[0] });
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenDelaysInSecondsContainsNegativeNumbers()
        {
            Assert.Throws<ArgumentException>(
                () => new AutomaticRetryAttribute { DelaysInSeconds = new [] { 1, -5 } });
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenDelayByAttemptIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new AutomaticRetryAttribute { DelayInSecondsByAttemptFunc = null });
        }

        [Fact]
        public void Ctor_SetsOnAttemptsExceededAction_ByDefault()
        {
            var filter = new AutomaticRetryAttribute();
            Assert.Equal(AttemptsExceededAction.Fail, filter.OnAttemptsExceeded);
        }

        [Fact]
        public void Ctor_DelayByAttemptIsNotNull_ByDefault()
        {
            var filter = new AutomaticRetryAttribute();
            Assert.NotNull(filter.DelayInSecondsByAttemptFunc);
        }

        [Fact]
        public void DelaysInSeconds_SetsValueCorrectly()
        {
            var filter = new AutomaticRetryAttribute { DelaysInSeconds = new[] { 5, 8 } };

            Assert.Equal(2, filter.DelaysInSeconds.Length);
            Assert.Equal(5, filter.DelaysInSeconds[0]);
            Assert.Equal(8, filter.DelaysInSeconds[1]);
        }

        [Fact]
        public void DelayInSecondsByAttemptFunc_ReturnCorrectValue_WhenCustomFunctionIsSet()
        {
            var filter = new AutomaticRetryAttribute { DelayInSecondsByAttemptFunc = attempt => (int)attempt % 3 };

            Assert.Equal(1, filter.DelayInSecondsByAttemptFunc(1));
            Assert.Equal(2, filter.DelayInSecondsByAttemptFunc(2));
            Assert.Equal(0, filter.DelayInSecondsByAttemptFunc(3));
            Assert.Equal(1, filter.DelayInSecondsByAttemptFunc(4));
            Assert.Equal(2, filter.DelayInSecondsByAttemptFunc(5));
            Assert.Equal(1, filter.DelayInSecondsByAttemptFunc(100));
        }

        [Fact]
        public void OnStateElection_ThrowsAnException_WhenDelayInSecondsByAttemptFuncThrowsAnException()
        {
            var exception = new Exception();
            
            var filter = new AutomaticRetryAttribute
            {
                DelayInSecondsByAttemptFunc = attempt =>
                {
                    throw exception;
                }
            };
            
            var thrownException = Assert.Throws<Exception>(() => filter.OnStateElection(_context.Object));

            Assert.Equal(exception, thrownException);
        }

        [Fact]
        public void OnStateElection_UsesDelaysInSeconds_WhenBothDelaysInSecondsAndDelayInSecondsByAttemptFuncAreSpecified()
        {
            var filter = new AutomaticRetryAttribute
            {
                DelayInSecondsByAttemptFunc = attempt => 1,
                DelaysInSeconds = new[] { 0 }
            };

            filter.OnStateElection(_context.Object);

            Assert.IsType<EnqueuedState>(_context.Object.CandidateState);
        }

        [Fact]
        public void OnStateElection_DoesNotChangeState_IfRetryAttemptsIsSetToZero()
        {
            var filter = new AutomaticRetryAttribute { Attempts = 0 };
            filter.OnStateElection(_context.Object);

            Assert.Same(_failedState, _context.Object.CandidateState);
        }

        [Fact]
        public void OnStateElection_ChangeStateToScheduled_IfRetryAttemptsWereNotExceeded()
        {
            var filter = CreateFilter();
            filter.OnStateElection(_context.Object);

            Assert.IsType<ScheduledState>(_context.Object.CandidateState);
            Assert.True(((ScheduledState)_context.Object.CandidateState).EnqueueAt > DateTime.UtcNow);

            Assert.NotNull(_context.Object.CandidateState.Reason);
            Assert.Contains("1 of 1", _context.Object.CandidateState.Reason);

            _connection.Verify(x => x.SetJobParameter(JobId, "RetryCount", "1"));
        }

        [Fact]
        public void OnStateElection_ChangeStateToEnqueued_IfDelayIsZero()
        {
            var filter = new AutomaticRetryAttribute
            {
                Attempts = 1,
                DelaysInSeconds = new[] { 0 }
            };
            
            filter.OnStateElection(_context.Object);

            Assert.IsType<EnqueuedState>(_context.Object.CandidateState);
            Assert.NotNull(_context.Object.CandidateState.Reason);
            Assert.Contains("1 of 1", _context.Object.CandidateState.Reason);

            _connection.Verify(x => x.SetJobParameter(JobId, "RetryCount", "1"));
        }

        [Fact]
        public void OnStateElection_DoesNotChangeAnything_IfCandidateStateIsNotFailedState()
        {
            var filter = CreateFilter();
            var state = new Mock<IState>();
            _context.ApplyContext.NewStateObject = null;
            _context.ApplyContext.NewState = state;

            filter.OnStateElection(_context.Object);

            Assert.Same(state.Object, _context.Object.CandidateState);
        }

        [Fact]
        public void OnStateElection_DoesNotChangeState_IfRetryAttemptsNumberExceeded()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "RetryCount")).Returns("1");
            var filter = CreateFilter();

            filter.OnStateElection(_context.Object);

            Assert.Same(_failedState, _context.Object.CandidateState);
        }

        [Fact]
        public void OnStateElection_ChangesStateToDeleted_IfRetryAttemptsNumberExceededAndOnAttemptsExceededIsSetToDelete()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "RetryCount")).Returns("1");
            var filter = new AutomaticRetryAttribute { Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete };

            filter.OnStateElection(_context.Object);

            Assert.IsType<DeletedState>(_context.Object.CandidateState);
        }

        [Fact]
        public void OnStateElection_ChangesStateToFailed_IfRetryAttemptsNumberExceededAndOnAttemptsExceedIsSetToFail()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "RetryCount")).Returns("1");
            var filter = new AutomaticRetryAttribute { Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Fail };

            filter.OnStateElection(_context.Object);

            Assert.IsType<FailedState>(_context.Object.CandidateState);
        }

        [Fact]
        public void OnStateElection_ChangesStateToDeleted_IfRetryAttemptsNumberIsZeroAndOnAttemptsExceedIsSetToDelete()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "RetryCount")).Returns("0");
            var filter = new AutomaticRetryAttribute { Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Delete };

            filter.OnStateElection(_context.Object);

            Assert.IsType<DeletedState>(_context.Object.CandidateState);
        }

        [Fact]
        public void OnStateApplied_AddsJobToRetriesSet_IfNewStateIsScheduled()
        {
            // Arrange
            var filter = CreateFilter();

            var newState = new ScheduledState(DateTime.UtcNow) { Reason = "Retry attempt ..." };
            var applyStateContext = CreatApplyStateContext(newState);

            // Act
            filter.OnStateApplied(applyStateContext, _transaction.Object);

            // Assert
            _transaction.Verify(t => t.AddToSet("retries", JobId));
        }

        [Fact]
        public void OnStateApplied_DoesNotAddJobToRetriesSet_IfNewStateIsScheduledAndReasonIsNull()
        {
            // Arrange
            var filter = CreateFilter();

            var newState = new ScheduledState(DateTime.UtcNow) { Reason = null };
            var applyStateContext = CreatApplyStateContext(newState);

            // Act
            filter.OnStateApplied(applyStateContext, _transaction.Object);

            // Assert
            _transaction.Verify(t => t.AddToSet(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void OnStateApplied_DoesNotAddJobToRetriesSet_IfNewStateIsScheduledAndReasonDoesNotMatch()
        {
            // Arrange
            var filter = CreateFilter();

            var newState = new ScheduledState(DateTime.UtcNow) { Reason = "Some reason." };
            var applyStateContext = CreatApplyStateContext(newState);

            // Act
            filter.OnStateApplied(applyStateContext, _transaction.Object);

            // Assert
            _transaction.Verify(t => t.AddToSet(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void OnStateApplied_DoesNotAddJobToRetriesSet_IfNewStateIsEnqueued()
        {
            // Arrange
            var filter = CreateFilter();

            var newState = new EnqueuedState { Reason = "Retry attempt ..." };
            var applyStateContext = CreatApplyStateContext(newState);
            
            // Act
            filter.OnStateApplied(applyStateContext, _transaction.Object);

            // Assert
            _transaction.Verify(t => t.AddToSet(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private static AutomaticRetryAttribute CreateFilter()
        {
            return new AutomaticRetryAttribute { Attempts = 1 };
        }

        private ApplyStateContext CreatApplyStateContext(IState newState)
        {
            var context = new ApplyStateContextMock();
            context.BackgroundJob.Id = JobId;
            context.Transaction = _transaction;
            context.NewStateObject = newState;
            return context.Object;
        }
    }
}
