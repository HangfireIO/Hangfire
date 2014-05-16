using System;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class EnqueuedStateHandlerFacts
    {
        private const string JobId = "1";
        private const string Queue = "critical";

        private readonly ApplyStateContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly Mock<IStorageConnection> _connection;

        public EnqueuedStateHandlerFacts()
        {
            _context = new ApplyStateContextMock();
            _context.StateContextValue.JobIdValue = JobId;
            _context.NewStateValue = new EnqueuedState { Queue = Queue };

            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection = new Mock<IStorageConnection>();
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

            _transaction.Verify(x => x.AddToQueue(Queue, JobId));
        }

        [Fact]
        public void Apply_ThrowsAnException_WhenOtherThanEnqueuedStateGiven()
        {
            var handler = new EnqueuedState.Handler();
            _context.NewStateValue = new Mock<IState>().Object;

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
