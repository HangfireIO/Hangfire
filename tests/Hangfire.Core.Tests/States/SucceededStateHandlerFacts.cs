using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class SucceededStateHandlerFacts
    {
        private readonly ApplyStateContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transactionMock
            = new Mock<IWriteOnlyTransaction>();

        public SucceededStateHandlerFacts()
        {
            _context = new ApplyStateContextMock();
        }

        [Fact]
        public void ShouldWorkOnlyWithSucceededState()
        {
            var handler = new SucceededState.Handler();
            Assert.Equal(SucceededState.StateName, handler.StateName);
        }

        [Fact]
        public void Apply_ShouldIncrease_SucceededCounter()
        {
            var handler = new SucceededState.Handler();
            handler.Apply(_context.Object, _transactionMock.Object);

            _transactionMock.Verify(x => x.IncrementCounter("stats:succeeded"), Times.Once);
        }

        [Fact]
        public void Unapply_ShouldDecrementStatistics()
        {
            var handler = new SucceededState.Handler();
            handler.Unapply(_context.Object, _transactionMock.Object);

            _transactionMock.Verify(x => x.DecrementCounter("stats:succeeded"), Times.Once);
        }
    }
}
