using System;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests
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
            _context.StateContextValue.JobIdValue = JobId;
            _context.ConnectionValue = _connection;
            _context.CandidateStateValue = _failedState;
        }

        [Fact]
        public void Ctor_SetsPositiveRetryAttemptsNumber_ByDefault()
        {
            var filter = new RetryAttribute();
            Assert.Equal(10, filter.Attempts);
        }

        [Fact]
        public void Ctor_SetsAllPropertyValuesCorrectly()
        {
            var filter = new RetryAttribute(175);
            Assert.Equal(175, filter.Attempts);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenAttemptsValueIsNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RetryAttribute(-1));
        }

        [Fact]
        public void OnStateElection_DoesNotChangeState_IfRetryAttemptsIsSetToZero()
        {
            var filter = new RetryAttribute(0);
            filter.OnStateElection(_context.Object);

            Assert.Same(_failedState, _context.Object.CandidateState);
        }

        [Fact]
        public void OnStateElection_ChangeStateToScheduled_IfRetryAttemptsWereNotExceeded()
        {
            var filter = new RetryAttribute(1);
            filter.OnStateElection(_context.Object);

            Assert.IsType<ScheduledState>(_context.Object.CandidateState);
            Assert.True(((ScheduledState)_context.Object.CandidateState).EnqueueAt > DateTime.UtcNow);
            Assert.Contains("1 of 1", _context.Object.CandidateState.Reason);

            _connection.Verify(x => x.SetJobParameter(JobId, "RetryCount", "1"));
        }

        [Fact]
        public void OnStateElection_DoesNotChangeAnything_IfCandidateStateIsNotFailedState()
        {
            var filter = new RetryAttribute(1);
            var state = new Mock<IState>();
            _context.CandidateStateValue = state.Object;

            filter.OnStateElection(_context.Object);
            
            Assert.Same(state.Object, _context.Object.CandidateState);
        }

        [Fact]
        public void OnStateElection_DoesNotChangeState_IfRetryAttemptsNumberExceeded()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "RetryCount")).Returns("1");
            var filter = new RetryAttribute(1);

            filter.OnStateElection(_context.Object);

            Assert.Same(_failedState, _context.Object.CandidateState);
        }
    }
}
