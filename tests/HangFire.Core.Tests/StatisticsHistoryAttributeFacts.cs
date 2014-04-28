using System;
using System.Globalization;
using System.Threading;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests
{
    public class StatisticsHistoryAttributeFacts
    {
        private const string JobId = "my-job";
        private const string CurrentState = "my-state";

        private readonly Mock<IStorageConnection> _connection;
        private readonly StateContext _stateContext;
        private readonly StatisticsHistoryAttribute _filter;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        public StatisticsHistoryAttributeFacts()
        {
            var job = Job.FromExpression(() => Method());

            _stateContext = new StateContext(JobId, job);
            _connection = new Mock<IStorageConnection>();
            _transaction = new Mock<IWriteOnlyTransaction>();

            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            _filter = new StatisticsHistoryAttribute();
        }

        [Fact]
        public void StatisticsHistoryFilter_ActsBefore_RetryFilter()
        {
            var statisticsHistoryFilter = new StatisticsHistoryAttribute();
            var retryFilter = new RetryAttribute();

            Assert.True(statisticsHistoryFilter.Order > retryFilter.Order);
        }

        [Fact]
        public void OnStateElection_IncrementsCounters_WithinTransaction()
        {
            // Arrange
            var context = new ElectStateContext(
                _stateContext, new SucceededState(), CurrentState, _connection.Object);
            
            // Act
            _filter.OnStateElection(context);

            // Assert
            _connection.Verify(x => x.CreateWriteTransaction(), Times.Once);
            _transaction.Verify(x => x.Dispose(), Times.Once);

            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void OnStateElection_IncrementsCounters_ForSucceededState()
        {
            var context = new ElectStateContext(
                _stateContext, new SucceededState(), CurrentState, _connection.Object);

            _filter.OnStateElection(context);

            VerifyCountersIncremented("stats:succeeded:");
        }

        [Fact]
        public void OnStateElection_IncrementsCounters_ForFailedState()
        {
            var context = new ElectStateContext(
                _stateContext, new FailedState(new AbandonedMutexException()), CurrentState, _connection.Object);

            _filter.OnStateElection(context);

            VerifyCountersIncremented("stats:failed:");
        }

        [Fact]
        public void OnStateElection_DoesNotCreateTransaction_ForUnsuitableState()
        {
            var context = new ElectStateContext(
                _stateContext, new ProcessingState("server"), CurrentState, _connection.Object);

            _filter.OnStateElection(context);

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

        public static void Method() { }
    }
}
