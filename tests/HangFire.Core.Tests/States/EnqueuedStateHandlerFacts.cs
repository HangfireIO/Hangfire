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
        private const string JobId = "1";
        private const string Queue = "critical";

        private readonly ApplyStateContext _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly StateContext _stateContext;
        private readonly Mock<IStorageConnection> _connection;

        public EnqueuedStateHandlerFacts()
        {
            var job = Job.FromExpression(() => Console.WriteLine());

            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection = new Mock<IStorageConnection>();
            _stateContext = new StateContext(JobId, job);

            _context = new ApplyStateContext(
                _connection.Object,
                _stateContext, 
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
        public void Apply_AddsJob_ToTheSpecifiedQueue()
        {
            var handler = new EnqueuedState.Handler();

            handler.Apply(_context, _transaction.Object);

            _transaction.Verify(x => x.AddToQueue(Queue, JobId));
        }

        [Fact]
        public void Apply_ThrowsAnException_WhenOtherThanEnqueuedStateGiven()
        {
            var handler = new EnqueuedState.Handler();
            var context = new ApplyStateContext(
                _connection.Object, _stateContext, new Mock<State>().Object, null);

            Assert.Throws<InvalidOperationException>(
                () => handler.Apply(context, _transaction.Object));
        }
    }
}
