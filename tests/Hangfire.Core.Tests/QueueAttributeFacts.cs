using Hangfire.States;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class QueueAttributeFacts
    {
        private readonly ElectStateContextMock _context;

        public QueueAttributeFacts()
        {
            _context = new ElectStateContextMock
            {
                ApplyContext = { NewStateObject = new EnqueuedState("queue") }
            };
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
            filter.OnStateElection(_context.Object);

            Assert.Equal("override", ((EnqueuedState)_context.Object.CandidateState).Queue);
        }

        [Fact]
        public void OnStateElection_DoesNotDoAnything_IfStateIsNotEnqueuedState()
        {
            var filter = new QueueAttribute("override");
            var context = new ElectStateContextMock
            {
                ApplyContext = { NewState = new Mock<IState>() }
            };

            // Does not throw
            filter.OnStateElection(context.Object);
        }
    }
}
