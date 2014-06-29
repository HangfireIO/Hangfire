using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class DeletedStateHandlerFacts
    {
        private readonly ApplyStateContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transactionMock
            = new Mock<IWriteOnlyTransaction>();

        public DeletedStateHandlerFacts()
        {
            _context = new ApplyStateContextMock();
        }

        [Fact]
        public void ShouldWorkOnlyWithDeletedState()
        {
            var handler = new DeletedState.Handler();
            Assert.Equal(DeletedState.StateName, handler.StateName);
        }

        [Fact]
        public void Apply_ShouldIncrease_DeletedCounter()
        {
            var handler = new DeletedState.Handler();
            handler.Apply(_context.Object, _transactionMock.Object);

            _transactionMock.Verify(x => x.IncrementCounter("stats:deleted"), Times.Once);
        }

        [Fact]
        public void Unapply_ShouldDecrementStatistics()
        {
            var handler = new DeletedState.Handler();
            handler.Unapply(_context.Object, _transactionMock.Object);

            _transactionMock.Verify(x => x.DecrementCounter("stats:deleted"), Times.Once);
        }
    }
}
