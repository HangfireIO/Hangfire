using System;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class ManualStateHandlerFacts
    {
        private const string Queue = "critical";

        private readonly ApplyStateContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        public ManualStateHandlerFacts()
        {
            _context = new ApplyStateContextMock
            {
                NewStateObject = new ManualState { Queue = Queue }
            };

            _transaction = new Mock<IWriteOnlyTransaction>();
        }

        [Fact]
        public void HandlerShouldBeRegistered_ForTheManualState()
        {
            var handler = new ManualState.Handler();
            Assert.Equal(ManualState.StateName, handler.StateName);
        }

        [Fact]
        public void Apply_AddsJob_ToTheSpecifiedQueue()
        {
            var handler = new ManualState.Handler();

            handler.Apply(_context.Object, _transaction.Object);

            _transaction.Verify(x => x.AddToQueue(Queue, _context.BackgroundJob.Id));
        }

        [Fact]
        public void Apply_ThrowsAnException_WhenOtherThanManualStateGiven()
        {
            var handler = new ManualState.Handler();
            _context.NewStateObject = null;
            _context.NewState = new Mock<IState>();

            Assert.Throws<InvalidOperationException>(
                () => handler.Apply(_context.Object, _transaction.Object));
        }

        [Fact]
        public void Unapply_DoesNotDoAnything()
        {
            var handler = new ManualState.Handler();

            // Does not throw
            handler.Unapply(null, null);
        }
    }
}
