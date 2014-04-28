using System;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Redis.Tests.States
{
    public class ProcessingStateHandlerFacts
    {
        private readonly ApplyStateContext _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction
            = new Mock<IWriteOnlyTransaction>();
        private const string JobId = "1";

        public ProcessingStateHandlerFacts()
        {
            var job = Job.FromExpression(() => Console.WriteLine());
            _context = new ApplyStateContext(
                new Mock<IStorageConnection>().Object,
                new StateContext(JobId, job),
                new ProcessingState("SomeServer"), 
                null);
        }

        [Fact]
        public void StateName_ShouldBeEqualToProcessingState()
        {
            var handler = new ProcessingStateHandler();
            Assert.Equal(ProcessingState.StateName, handler.StateName);
        }

        [Fact]
        public void Apply_ShouldAddTheJob_ToTheProcessingSet()
        {
            var handler = new ProcessingStateHandler();
            handler.Apply(_context, _transaction.Object);

            _transaction.Verify(x => x.AddToSet(
                "processing", JobId, It.IsAny<double>()));
        }

        [Fact]
        public void Unapply_ShouldRemoveTheJob_FromTheProcessingSet()
        {
            var handler = new ProcessingStateHandler();
            handler.Unapply(_context, _transaction.Object);

            _transaction.Verify(x => x.RemoveFromSet("processing", JobId));
        }
    }
}
