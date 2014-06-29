using Hangfire.Core.Tests;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Redis.Tests
{
    public class DeletedStateHandlerFacts
    {
        private const string JobId = "1";

        private readonly ApplyStateContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        public DeletedStateHandlerFacts()
        {
            _context = new ApplyStateContextMock();
            _context.StateContextValue.JobIdValue = JobId;
            _context.NewStateValue = new DeletedState();

            _transaction = new Mock<IWriteOnlyTransaction>();
        }

        [Fact]
        public void StateName_ShouldBeEqualToSucceededState()
        {
            var handler = new DeletedStateHandler();
            Assert.Equal(DeletedState.StateName, handler.StateName);
        }

        [Fact]
        public void Apply_ShouldInsertTheJob_ToTheBeginningOfTheSucceededList_AndTrimIt()
        {
            var handler = new DeletedStateHandler();
            handler.Apply(_context.Object, _transaction.Object);

            _transaction.Verify(x => x.InsertToList(
                "deleted", JobId));
            _transaction.Verify(x => x.TrimList(
                "deleted", 0, 99));
        }

        [Fact]
        public void Unapply_ShouldRemoveTheJob_FromTheSucceededList()
        {
            var handler = new DeletedStateHandler();
            handler.Unapply(_context.Object, _transaction.Object);

            _transaction.Verify(x => x.RemoveFromList("deleted", JobId));
        }
    }
}
