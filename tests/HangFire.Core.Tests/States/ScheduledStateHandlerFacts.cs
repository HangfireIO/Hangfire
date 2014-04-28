using System;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class ScheduledStateHandlerFacts
    {
        private readonly ApplyStateContext _context;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        private const string JobId = "1";
        private readonly DateTime EnqueueAt = DateTime.UtcNow.AddDays(1);
        private readonly Mock<IStorageConnection> _connection;
        private readonly StateContext _stateContext;

        public ScheduledStateHandlerFacts()
        {
            var job = Job.FromExpression(() => Console.WriteLine());

            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection = new Mock<IStorageConnection>();
            _stateContext = new StateContext(JobId, job);
            _context = new ApplyStateContext(
                _connection.Object,
                _stateContext, 
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

        [Fact]
        public void Apply_ThrowsAnException_WhenGivenStateIsNotScheduledState()
        {
            var handler = new ScheduledState.Handler();
            var context = new ApplyStateContext(
                _connection.Object, _stateContext, new Mock<State>().Object, null);

            Assert.Throws<InvalidOperationException>(
                () => handler.Apply(context, _transaction.Object));
        }
    }
}
