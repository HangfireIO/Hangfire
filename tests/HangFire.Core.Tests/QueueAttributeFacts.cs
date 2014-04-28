using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests
{
    public class QueueAttributeFacts
    {
        private readonly StateContext _stateContext;
        private readonly Mock<IStorageConnection> _connection;
        private readonly ElectStateContext _context;

        public QueueAttributeFacts()
        {
            _stateContext = new StateContext("id", Job.FromExpression(() => Sample()));
            _connection = new Mock<IStorageConnection>();
            var enqueuedState = new EnqueuedState("queue");

            _context = new ElectStateContext(_stateContext, enqueuedState, null, _connection.Object);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllPropertyValues()
        {
            var filter = new QueueAttribute("hello");
            Assert.Equal("hello", filter.Queue);
        }

        [Fact]
        public void OnStateElection_OverridesTheQueue_OfTheCandidateState()
        {
            var filter = new QueueAttribute("override");
            filter.OnStateElection(_context);

            Assert.Equal("override", ((EnqueuedState)_context.CandidateState).Queue);
        }

        [Fact]
        public void OnStateElection_DoesNotDoAnything_IfStateIsNotEnqueuedState()
        {
            var filter = new QueueAttribute("override");
            var context = new ElectStateContext(_context, new Mock<State>().Object, null, _connection.Object);

            Assert.DoesNotThrow(() => filter.OnStateElection(context));
        }

        public static void Sample() { }
    }
}
