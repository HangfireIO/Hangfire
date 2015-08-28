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

        public RetryAttributeFacts()
        {
            _failedState = new FailedState(new InvalidOperationException());
            _connection = new Mock<IStorageConnection>();

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
        public void Ctor_SetsOnAttemptsExceededAction_ByDefault()
        {
            var filter = new AutomaticRetryAttribute();
            Assert.Equal(AttemptsExceededAction.Fail, filter.OnAttemptsExceeded);
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

        private static AutomaticRetryAttribute CreateFilter()
        {
            return new AutomaticRetryAttribute { Attempts = 1 };
        }
    }
}
