using System;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class ScheduledStateHandlerFacts
    {
        private readonly ApplyStateContextMock _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        private const string JobId = "1";
        private readonly DateTime _enqueueAt = DateTime.UtcNow.AddDays(1);

        public ScheduledStateHandlerFacts()
        {
            _context = new ApplyStateContextMock();
            _context.BackgroundJob.Id = JobId;
            _context.NewStateObject = new ScheduledState(_enqueueAt);

            _transaction = new Mock<IWriteOnlyTransaction>();
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
            handler.Apply(_context.Object, _transaction.Object);

            _transaction.Verify(x => x.AddToSet(
                "schedule", JobId, JobHelper.ToTimestamp(_enqueueAt)));
        }

        [Fact]
        public void Unapply_ShouldRemoveTheJob_FromTheScheduledSet()
        {
            var handler = new ScheduledState.Handler();
            handler.Unapply(_context.Object, _transaction.Object);

            _transaction.Verify(x => x.RemoveFromSet("schedule", JobId));
        }

        [Fact]
        public void Apply_ThrowsAnException_WhenGivenStateIsNotScheduledState()
        {
            var handler = new ScheduledState.Handler();
            _context.NewStateObject = null;
            _context.NewState = new Mock<IState>();

            Assert.Throws<InvalidOperationException>(
                () => handler.Apply(_context.Object, _transaction.Object));
        }
    }
}
