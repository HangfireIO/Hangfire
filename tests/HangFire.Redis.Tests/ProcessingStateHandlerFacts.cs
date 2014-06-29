using System;
using Hangfire.Common;
using Hangfire.Core.Tests;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Redis.Tests
{
    public class ProcessingStateHandlerFacts
    {
        private const string JobId = "1";

        private readonly ApplyStateContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        
        public ProcessingStateHandlerFacts()
        {
            _context = new ApplyStateContextMock();
            _context.StateContextValue.JobIdValue = JobId;
            _context.NewStateValue = new ProcessingState("server", 1);

            _transaction = new Mock<IWriteOnlyTransaction>();
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
            handler.Apply(_context.Object, _transaction.Object);

            _transaction.Verify(x => x.AddToSet(
                "processing", JobId, It.IsAny<double>()));
        }

        [Fact]
        public void Unapply_ShouldRemoveTheJob_FromTheProcessingSet()
        {
            var handler = new ProcessingStateHandler();
            handler.Unapply(_context.Object, _transaction.Object);

            _transaction.Verify(x => x.RemoveFromSet("processing", JobId));
        }
    }
}
