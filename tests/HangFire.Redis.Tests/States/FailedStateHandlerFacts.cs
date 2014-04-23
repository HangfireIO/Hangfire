using System;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Redis.States;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Redis.Tests.States
{
    public class FailedStateHandlerFacts
    {
        private readonly ApplyStateContext _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction
            = new Mock<IWriteOnlyTransaction>();
        private const string JobId = "1";

        public FailedStateHandlerFacts()
        {
            var job = Job.FromExpression(() => Console.WriteLine());
            _context = new ApplyStateContext(
                new Mock<IStorageConnection>().Object,
                new StateContext(JobId, job),
                new FailedState(new Exception()),
                null);
        }

        [Fact]
        public void StateName_ShouldBeEqualToFailedState()
        {
            var handler = new FailedStateHandler();
            Assert.Equal(FailedState.StateName, handler.StateName);
        }

        [Fact]
        public void Apply_ShouldAddTheJob_ToTheFailedSet()
        {
            var handler = new FailedStateHandler();
            handler.Apply(_context, _transaction.Object);

            _transaction.Verify(x => x.AddToSet(
                "failed", JobId, It.IsAny<double>()));
        }

        [Fact]
        public void Unapply_ShouldRemoveTheJob_FromTheFailedSet()
        {
            var handler = new FailedStateHandler();
            handler.Unapply(_context, _transaction.Object);

            _transaction.Verify(x => x.RemoveFromSet("failed", JobId));
        }
    }
}
