using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class AwaitingStateHandlerFacts
    {
        private readonly ApplyStateContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transactionMock
            = new Mock<IWriteOnlyTransaction>();

        public AwaitingStateHandlerFacts()
        {
            _context = new ApplyStateContextMock();
        }

        [Fact]
        public void ShouldWorkOnlyWithAwaitingState()
        {
            var handler = new AwaitingState.Handler();
            Assert.Equal(AwaitingState.StateName, handler.StateName);
        }

        [Fact]
        public void Apply_ShouldAddToSet_Awaiting()
        {
            var handler = new AwaitingState.Handler();
            handler.Apply(_context.Object, _transactionMock.Object);

            _transactionMock.Verify(x => x.AddToSet("awaiting", "JobId", It.IsAny<double>()), Times.Once);
        }

        [Fact]
        public void Unapply_ShouldRemoveFromSet_Awaiting()
        {
            var handler = new AwaitingState.Handler();
            handler.Unapply(_context.Object, _transactionMock.Object);

            _transactionMock.Verify(x => x.RemoveFromSet("awaiting", "JobId"), Times.Once);
        }
    }
}
