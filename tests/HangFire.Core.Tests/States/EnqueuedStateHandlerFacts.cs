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
        private readonly StateApplyingContext _context;
        private readonly Mock<IWriteOnlyTransaction> _transactionMock
            = new Mock<IWriteOnlyTransaction>();

        private readonly EnqueuedState _state;

        private const string JobId = "1";

        public EnqueuedStateHandlerFacts()
        {
            var methodData = MethodData.FromExpression(() => Console.WriteLine());
            
            _state = new EnqueuedState();
            _context = new StateApplyingContext(
                new StateContext(JobId, methodData),
                new Mock<IStorageConnection>().Object,
                _state,
                null);
        }

        [Fact]
        public void HandlerShouldBeRegistered_ForTheEnqueuedState()
        {
            var handler = new EnqueuedState.Handler();
            Assert.Equal(EnqueuedState.StateName, handler.StateName);
        }

        [Fact]
        public void Apply_ShouldAddJobToTheDefaultQueue_WhenTheJobMethodIsUndecorated()
        {
            var handler = new EnqueuedState.Handler();

            handler.Apply(_context, _transactionMock.Object);

            _transactionMock.Verify(
                x => x.AddToQueue(EnqueuedState.DefaultQueue, JobId));
        }

        // TODO: add more tests

        [Fact]
        public void OnStateChanging_ShouldMutateStateData_WithTheQueueName()
        {
            /*var handler = new EnqueuedState.Handler();

            handler.OnStateChanging(_changingContext);
            // TODO: should be added to global collection
            Assert.True(_state.Object.Data.ContainsKey("Queue"));
            Assert.Equal(EnqueuedState.DefaultQueue, _state.Object.Data["Queue"]);*/
        }
    }
}
