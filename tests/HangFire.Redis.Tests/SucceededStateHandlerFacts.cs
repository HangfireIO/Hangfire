using System;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Redis.Tests
{
    public class SucceededStateHandlerFacts
    {
        private readonly ApplyStateContext _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction
            = new Mock<IWriteOnlyTransaction>();
        private const string JobId = "1";

        public SucceededStateHandlerFacts()
        {
            var job = Job.FromExpression(() => Console.WriteLine());
            _context = new ApplyStateContext(
                new Mock<IStorageConnection>().Object,
                new StateContext(JobId, job),
                new SucceededState(),
                null);
        }

        [Fact]
        public void StateName_ShouldBeEqualToSucceededState()
        {
            var handler = new SucceededStateHandler();
            Assert.Equal(SucceededState.StateName, handler.StateName);
        }

        [Fact]
        public void Apply_ShouldInsertTheJob_ToTheBeginningOfTheSucceededList_AndTrimIt()
        {
            var handler = new SucceededStateHandler();
            handler.Apply(_context, _transaction.Object);

            _transaction.Verify(x => x.InsertToList(
                "succeeded", JobId));
            _transaction.Verify(x => x.TrimList(
                "succeeded", 0, 99));
        }

        [Fact]
        public void Unapply_ShouldRemoveTheJob_FromTheSucceededList()
        {
            var handler = new SucceededStateHandler();
            handler.Unapply(_context, _transaction.Object);

            _transaction.Verify(x => x.RemoveFromList("succeeded", JobId));
        }
    }
}
