using System;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests
{
    public class StatisticsHistoryAttributeFacts
    {
        private readonly Mock<IStorageConnection> _connection;
        private readonly StatisticsHistoryAttribute _filter;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly ElectStateContextMock _context;

        public StatisticsHistoryAttributeFacts()
        {
            _connection = new Mock<IStorageConnection>();

            _context = new ElectStateContextMock();
            _context.StateContextValue.ConnectionValue = _connection;
            _context.CandidateStateValue = new SucceededState();
            
            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            _filter = new StatisticsHistoryAttribute();
        }

        [Fact]
        public void StatisticsHistoryFilter_ActsBefore_RetryFilter()
        {
            var statisticsHistoryFilter = new StatisticsHistoryAttribute();
            var retryFilter = new AutomaticRetryAttribute();

            Assert.True(statisticsHistoryFilter.Order > retryFilter.Order);
        }

        [Fact]
        public void OnStateElection_IncrementsCounters_WithinTransaction()
        {
            _filter.OnStateElection(_context.Object);

            _connection.Verify(x => x.CreateWriteTransaction(), Times.Once);
            _transaction.Verify(x => x.Dispose(), Times.Once);
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void OnStateElection_IncrementsCounters_ForSucceededState()
        {
            _filter.OnStateElection(_context.Object);

            VerifyCountersIncremented("stats:succeeded:");
        }

        [Fact]
        public void OnStateElection_IncrementsCounters_ForFailedState()
        {
            _context.CandidateStateValue = new FailedState(new InvalidOperationException());

            _filter.OnStateElection(_context.Object);

            VerifyCountersIncremented("stats:failed:");
        }

        [Fact]
        public void OnStateElection_DoesNotCreateTransaction_ForUnsuitableState()
        {
            _context.CandidateStateValue = new ProcessingState("server");

            _filter.OnStateElection(_context.Object);

            _connection.Verify(x => x.CreateWriteTransaction(), Times.Never);
        }

        private void VerifyCountersIncremented(string prefix)
        {
            var thisDay = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var prevDay = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

            var thisHour = DateTime.UtcNow.ToString("yyyy-MM-dd-HH");
            var prevHour = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-dd-HH");

            _transaction.Verify(x => x.IncrementCounter(
                It.Is<string>(key => key == prefix + thisDay || key == prefix + prevDay),
                It.Is<TimeSpan>(expire => expire.TotalDays >= 28)));

            _transaction.Verify(x => x.IncrementCounter(
                It.Is<string>(date => date == prefix + thisHour || date == prefix + prevHour),
                TimeSpan.FromDays(1)));
        }
    }
}
