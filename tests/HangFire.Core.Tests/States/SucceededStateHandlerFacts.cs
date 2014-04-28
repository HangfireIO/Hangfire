using System;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class SucceededStateHandlerFacts
    {
        private readonly ApplyStateContext _context;
        private readonly Mock<IWriteOnlyTransaction> _transactionMock
            = new Mock<IWriteOnlyTransaction>();

        public SucceededStateHandlerFacts()
        {
            var job = Job.FromExpression(() => Console.WriteLine());

            _context = new ApplyStateContext(
                new Mock<IStorageConnection>().Object,
                new StateContext("1", job), 
                new SucceededState(), 
                null);
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
            handler.Apply(_context, _transactionMock.Object);

            _transactionMock.Verify(x => x.IncrementCounter("stats:succeeded"), Times.Once);
        }

        [Fact]
        public void Unapply_ShouldDecrementStatistics()
        {
            var handler = new SucceededState.Handler();
            handler.Unapply(_context, _transactionMock.Object);

            _transactionMock.Verify(x => x.DecrementCounter("stats:succeeded"), Times.Once);
        }
    }
}
