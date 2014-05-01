using System;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests
{
    public class RetryAttributeFacts
    {
        private const string JobId = "id";
        private const string CurrentState = "State";

        private readonly StateContext _stateContext;
        private readonly FailedState _failedState;
        private readonly Mock<IStorageConnection> _connection;
        private readonly ElectStateContext _context;

        public RetryAttributeFacts()
        {
            var job = Job.FromExpression(() => Sample());
            _stateContext = new StateContext(JobId, job);
            _failedState = new FailedState(new InvalidOperationException());
            _connection = new Mock<IStorageConnection>();
            _context = new ElectStateContext(_stateContext, _failedState, CurrentState, _connection.Object);
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
            filter.OnStateElection(_context);

            Assert.Same(_failedState, _context.CandidateState);
        }

        [Fact]
        public void OnStateElection_ChangeStateToScheduled_IfRetryAttemptsWereNotExceeded()
        {
            var filter = new RetryAttribute(1);
            filter.OnStateElection(_context);

            Assert.IsType<ScheduledState>(_context.CandidateState);
            Assert.True(((ScheduledState)_context.CandidateState).EnqueueAt > DateTime.UtcNow);
            Assert.Contains("1 of 1", _context.CandidateState.Reason);

            _connection.Verify(x => x.SetJobParameter(JobId, "RetryCount", "1"));
        }

        [Fact]
        public void OnStateElection_DoesNotChangeAnything_IfCandidateStateIsNotFailedState()
        {
            var filter = new RetryAttribute(1);
            var state = new Mock<IState>();
            var context = new ElectStateContext(_stateContext, state.Object, CurrentState, _connection.Object);

            filter.OnStateElection(context);
            
            Assert.Same(state.Object, context.CandidateState);
        }

        [Fact]
        public void OnStateElection_DoesNotChangeState_IfRetryAttemptsNumberExceeded()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "RetryCount")).Returns("1");
            var filter = new RetryAttribute(1);

            filter.OnStateElection(_context);

            Assert.Same(_failedState, _context.CandidateState);
        }

        public static void Sample() { }
    }
}
