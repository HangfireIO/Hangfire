using System;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class EnqueuedStateHandlerFacts
    {
        private const string Queue = "critical";

        private readonly ApplyStateContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        public EnqueuedStateHandlerFacts()
        {
            _context = new ApplyStateContextMock();
            _context.NewStateObject = new EnqueuedState { Queue = Queue };

            _transaction = new Mock<IWriteOnlyTransaction>();
        }

        [Fact]
        public void HandlerShouldBeRegistered_ForTheEnqueuedState()
        {
            var handler = new EnqueuedState.Handler();
            Assert.Equal(EnqueuedState.StateName, handler.StateName);
        }

        [Fact]
        public void Apply_AddsJob_ToTheSpecifiedQueue()
        {
            var handler = new EnqueuedState.Handler();

            handler.Apply(_context.Object, _transaction.Object);

            _transaction.Verify(x => x.AddToQueue(Queue, _context.BackgroundJob.Id));
        }

        [Fact]
        public void Apply_ThrowsAnException_WhenOtherThanEnqueuedStateGiven()
        {
            var handler = new EnqueuedState.Handler();
            _context.NewStateObject = null;
            _context.NewState = new Mock<IState>();

            Assert.Throws<InvalidOperationException>(
                () => handler.Apply(_context.Object, _transaction.Object));
        }

        [Fact]
        public void Unapply_DoesNotDoAnything()
        {
            var handler = new EnqueuedState.Handler();

            Assert.DoesNotThrow(() => handler.Unapply(null, null));
        }
    }
}
