using System;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class ScheduledStateHandlerFacts
    {
        private readonly ApplyStateContext _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction
            = new Mock<IWriteOnlyTransaction>();

        private const string JobId = "1";
        private readonly DateTime EnqueueAt = DateTime.UtcNow.AddDays(1);

        public ScheduledStateHandlerFacts()
        {
            var methodData = MethodData.FromExpression(() => Console.WriteLine());
            _context = new ApplyStateContext(
                new Mock<IStorageConnection>().Object,
                new StateContext(JobId, methodData), 
                new ScheduledState(EnqueueAt), 
                null);
        }

        [Fact]
        public void StateName_ShouldBeEqualToScheduledState()
        {
            var handler = new ScheduledState.Handler();
            Assert.Equal(ScheduledState.StateName, handler.StateName);
        }

        [Fact]
        public void Apply_ShouldAddTheJob_ToTheScheduleSet_WithTheCorrectScore()
        {
            var handler = new ScheduledState.Handler();
            handler.Apply(_context, _transaction.Object);

            _transaction.Verify(x => x.AddToSet(
                "schedule", JobId, JobHelper.ToTimestamp(EnqueueAt)));
        }

        [Fact]
        public void Unapply_ShouldRemoveTheJob_FromTheScheduledSet()
        {
            var handler = new ScheduledState.Handler();
            handler.Unapply(_context, _transaction.Object);

            _transaction.Verify(x => x.RemoveFromSet("schedule", JobId));
        }
    }
}
