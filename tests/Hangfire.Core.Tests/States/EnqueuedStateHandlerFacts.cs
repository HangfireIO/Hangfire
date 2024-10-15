using System;
using Hangfire.Common;
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
        private readonly EnqueuedState _enqueuedState;

        public EnqueuedStateHandlerFacts()
        {
            _enqueuedState = new EnqueuedState { Queue = Queue };
            _context = new ApplyStateContextMock
            {
                NewStateObject = _enqueuedState
            };

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
        public void Apply_WithJobAndQueueSpecified_ThrowsAnException_WhenRequiredFeatureNotSupported()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => BackgroundJobMock.SomeMethod(), "critical");
            var handler = new EnqueuedState.Handler();

            Assert.Throws<NotSupportedException>(
                () => handler.Apply(_context.Object, _transaction.Object));
        }

        [Fact]
        public void Apply_AddsJob_ToTheJobTargetQueue_WhenEnqueuedState_HasTheDefaultQueue()
        {
            _context.Storage.Setup(x => x.HasFeature("Job.Queue")).Returns(true);
            _context.BackgroundJob.Job = Job.FromExpression(() => BackgroundJobMock.SomeMethod(), "myqueue");
            _enqueuedState.Queue = "default";
            var handler = new EnqueuedState.Handler();

            handler.Apply(_context.Object, _transaction.Object);

            _transaction.Verify(x => x.AddToQueue("myqueue", _context.BackgroundJob.Id));
        }

        [Fact]
        public void Apply_AddsJobToTheOverridenQueue_WhenTheJobTargetQueuePresent_ButEnqueuedStateQueueIsNotDefault()
        {
            _context.Storage.Setup(x => x.HasFeature("Job.Queue")).Returns(true);
            _context.BackgroundJob.Job = Job.FromExpression(() => BackgroundJobMock.SomeMethod(), "myqueue");
            _enqueuedState.Queue = "otherqueue";
            var handler = new EnqueuedState.Handler();

            handler.Apply(_context.Object, _transaction.Object);

            _transaction.Verify(x => x.AddToQueue("otherqueue", _context.BackgroundJob.Id));
        }

        [Fact]
        public void Unapply_DoesNotDoAnything()
        {
            var handler = new EnqueuedState.Handler();

            // Does not throw
            handler.Unapply(null, null);
        }
    }
}
