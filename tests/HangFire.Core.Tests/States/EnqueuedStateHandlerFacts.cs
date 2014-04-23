using System;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class EnqueuedStateHandlerFacts
    {
        private readonly ApplyStateContext _context;
        private readonly Mock<IWriteOnlyTransaction> _transactionMock
            = new Mock<IWriteOnlyTransaction>();

        private const string JobId = "1";
        private const string Queue = "critical";

        public EnqueuedStateHandlerFacts()
        {
            var job = Job.FromExpression(() => Console.WriteLine());
            _context = new ApplyStateContext(
                new Mock<IStorageConnection>().Object,
                new StateContext(JobId, job), 
                new EnqueuedState { Queue = Queue }, 
                null);
        }

        [Fact]
        public void HandlerShouldBeRegistered_ForTheEnqueuedState()
        {
            var handler = new EnqueuedState.Handler();
            Assert.Equal(EnqueuedState.StateName, handler.StateName);
        }

        [Fact]
        public void Apply_ShouldAddJob_ToTheSpecifiedQueue()
        {
            var handler = new EnqueuedState.Handler();

            handler.Apply(_context, _transactionMock.Object);

            _transactionMock.Verify(
                x => x.AddToQueue(Queue, JobId));
        }
    }
}
