using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using NCrontab;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class RecurringJobSchedulerFacts
    {
        private const string RecurringJobId = "recurring-job-id";

        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IBackgroundJobClient> _client;
        private readonly CancellationToken _token;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Dictionary<string, string> _recurringJob;
        private readonly Mock<IDateTimeProvider> _dateTimeProvider;
        private DateTime _currentTime;
        private readonly DateTime _nextTime;

        public RecurringJobSchedulerFacts()
        {
            _storage = new Mock<JobStorage>();
            _client = new Mock<IBackgroundJobClient>();
            _dateTimeProvider = new Mock<IDateTimeProvider>();
            _token = new CancellationToken();

            // Setting up the successful path
            _currentTime = new DateTime(2012, 12, 12, 12, 12, 0);
            _nextTime = _currentTime.AddHours(1);

            _dateTimeProvider.Setup(x => x.CurrentDateTime).Returns(() => _currentTime);
            _dateTimeProvider.Setup(x => x.GetNextOccurrence(It.IsNotNull<CrontabSchedule>()))
                .Returns(() => _nextTime);

            _recurringJob = new Dictionary<string, string>
            {
                { "NextExecution", JobHelper.SerializeDateTime(_currentTime) },
                { "Cron", "* * * * *" },
                { "Job", JobHelper.ToJson(InvocationData.Serialize(Job.FromExpression(() => Console.WriteLine()))) }
            };

            _connection = new Mock<IStorageConnection>();
            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _connection.Setup(x => x.GetAllItemsFromSet("recurring-jobs"))
                .Returns(new HashSet<string> { RecurringJobId });

            _connection.Setup(x => x.GetAllEntriesFromHash(String.Format("recurring-job:{0}", RecurringJobId)))
                .Returns(_recurringJob);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RecurringJobScheduler(null, _client.Object, _dateTimeProvider.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RecurringJobScheduler(_storage.Object, null, _dateTimeProvider.Object));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenDateTimeProviderIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RecurringJobScheduler(_storage.Object, _client.Object, null));

            Assert.Equal("dateTimeProvider", exception.ParamName);
        }

        [Fact]
        public void Execute_EnqueuesRecurringJob_AndUpdatesIt_WhenNextExecutionTime_IsEqualToCurrentTime()
        {
            _client.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-id");

            var scheduler = CreateScheduler();

            scheduler.Execute(_token);

            _client.Verify(x => x.Create(It.IsNotNull<Job>(), It.IsAny<EnqueuedState>()));

            _connection.Verify(x => x.SetRangeInHash(
                String.Format("recurring-job:{0}", RecurringJobId),
                It.Is<Dictionary<string, string>>(rj =>
                    rj["LastExecution"] == JobHelper.SerializeDateTime(_currentTime)
                 && rj["LastJobId"] == "job-id"
                 && rj["NextExecution"] == JobHelper.SerializeDateTime(_nextTime))));
        }

        [Fact]
        public void Execute_DoesNotEnqueueRecurringJob_AndDoesNotUpdateIt_WhenNextExecutionTime_IsInTheFuture()
        {
            _recurringJob["NextExecution"] = JobHelper.SerializeDateTime(_currentTime.AddDays(1));
            var scheduler = CreateScheduler();

            scheduler.Execute(_token);

            _client.Verify(
                x => x.Create(It.IsAny<Job>(), It.IsAny<EnqueuedState>()),
                Times.Never);

            _connection.Verify(
                x => x.SetRangeInHash(
                    It.IsAny<string>(),
                    It.IsAny<IEnumerable<KeyValuePair<string, string>>>()),
                Times.Never);
        }

        [Fact]
        public void Execute_EnqueuesRecurringJob_WhenNextExecutionTime_IsInThePast()
        {
            _recurringJob["NextExecution"] = JobHelper.SerializeDateTime(_currentTime.AddDays(-1));
            var scheduler = CreateScheduler();

            scheduler.Execute(_token);

            _client.Verify(x => x.Create(It.IsNotNull<Job>(), It.IsAny<EnqueuedState>()));
        }

        [Fact]
        public void Execute_DoesNotFail_WhenRecurringJobDoesNotExist()
        {
            _connection.Setup(x => x.GetAllItemsFromSet(It.IsAny<string>()))
                .Returns(new HashSet<string> { "non-existing-job" });
            var scheduler = CreateScheduler();

            Assert.DoesNotThrow(() => scheduler.Execute(_token));
        }

        [Fact]
        public void Execute_SetsTheNextExecutionTime_WhenItIsNull()
        {
            _recurringJob.Remove("NextExecution");
            var scheduler = CreateScheduler();

            scheduler.Execute(_token);

            _connection.Setup(x => x.SetRangeInHash(
                String.Format("recurring-job:{0}", RecurringJobId),
                It.Is<Dictionary<string, string>>(rj =>
                    rj["NextExecution"] == JobHelper.SerializeDateTime(_nextTime))));
        }

        [Fact]
        public void Execute_WorksWithOneMinuteInterval()
        {
            _dateTimeProvider.Setup(x => x.CurrentDateTime).Returns(
                new DateTime(2012, 12, 12, 12, 12, 12));
            var scheduler = CreateScheduler();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            Assert.Throws<OperationCanceledException>(() => scheduler.Execute(cts.Token));
        }

        [Fact]
        public void Execute_HandlesJobLoadException()
        {
            // Arrange
            _recurringJob["Job"] =
                JobHelper.ToJson(new InvocationData("SomeType", "SomeMethod", "Parameters", "arguments"));

            var scheduler = CreateScheduler();

            // Act & Assert
            Assert.DoesNotThrow(() => scheduler.Execute(_token));
        }

        private RecurringJobScheduler CreateScheduler()
        {
            return new RecurringJobScheduler(_storage.Object, _client.Object, _dateTimeProvider.Object);
        }
    }
}
