using System;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests
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

            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            _filter = new StatisticsHistoryAttribute();

            _context = new ElectStateContextMock
            {
                ApplyContext =
                {
                    Connection = _connection,
                    NewStateObject = new SucceededState(null, 11, 123),
                    Transaction = _transaction
                }
            };
        }

        [Fact]
        public void StatisticsHistoryFilter_ActsBefore_RetryFilter()
        {
            var statisticsHistoryFilter = new StatisticsHistoryAttribute();
            var retryFilter = new AutomaticRetryAttribute();

            Assert.True(statisticsHistoryFilter.Order > retryFilter.Order);
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
            _context.ApplyContext.NewStateObject = new FailedState(new InvalidOperationException());

            _filter.OnStateElection(_context.Object);

            VerifyCountersIncremented("stats:failed:");
        }

        [Fact]
        public void OnStateElection_DoesNotCreateTransaction_ForUnsuitableState()
        {
            _context.ApplyContext.NewStateObject = new ProcessingState("server", "1");

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
                It.Is<TimeSpan>(expire => expire.TotalDays >= 27)));

            _transaction.Verify(x => x.IncrementCounter(
                It.Is<string>(date => date == prefix + thisHour || date == prefix + prevHour),
                TimeSpan.FromDays(1)));
        }
    }
}
