using System;
using System.Linq;
using System.Threading;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class SchedulePollerFacts
    {
        private const string JobId = "id";
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IStateChangeProcess> _process;
        private readonly BackgroundProcessContextMock _context;

        public SchedulePollerFacts()
        {
            _context = new BackgroundProcessContextMock();
            _context.CancellationTokenSource.Cancel();

            _connection = new Mock<IStorageConnection>();
            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _process = new Mock<IStateChangeProcess>();

            _connection.Setup(x => x.GetFirstByLowestScoreFromSet(
                "schedule", 0, It.Is<double>(time => time > 0))).Returns(JobId);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SchedulePoller(Timeout.InfiniteTimeSpan, null));

            Assert.Equal("process", exception.ParamName);
        }

        [Fact]
        public void Execute_MovesJobStateToEnqueued()
        {
            var scheduler = CreateScheduler();

			scheduler.Execute(_context.Object);

            _process.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == JobId &&
                ctx.NewState is EnqueuedState &&
                ctx.ExpectedStates.SequenceEqual(new[] { ScheduledState.StateName }))));

            _connection.Verify(x => x.Dispose());
        }

        [Fact]
        public void Execute_DoesNotCallStateChangeProcess_IfThereAreNoJobsToEnqueue()
        {
            _connection.Setup(x => x.GetFirstByLowestScoreFromSet(
                "schedule", 0, It.Is<double>(time => time > 0))).Returns((string)null);
            var scheduler = CreateScheduler();

			scheduler.Execute(_context.Object);

            _process.Verify(
                x => x.ChangeState(It.IsAny<StateChangeContext>()),
                Times.Never);
        }

        private SchedulePoller CreateScheduler()
        {
            return new SchedulePoller(Timeout.InfiniteTimeSpan, _process.Object);
        }
    }
}
