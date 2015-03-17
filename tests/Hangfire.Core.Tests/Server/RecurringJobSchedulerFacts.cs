using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Mock<IScheduleInstantFactory> _instantFactory; 
        private readonly Mock<IThrottler> _throttler;
        private readonly Mock<IScheduleInstant> _instant;
        private readonly TimeZoneInfo _timeZone;

        public RecurringJobSchedulerFacts()
        {
            _storage = new Mock<JobStorage>();
            _client = new Mock<IBackgroundJobClient>();
            _instantFactory = new Mock<IScheduleInstantFactory>();
            _throttler = new Mock<IThrottler>();
            _token = new CancellationTokenSource().Token;

            // Setting up the successful path
            _instant = new Mock<IScheduleInstant>();
            _instant.Setup(x => x.GetNextInstants(It.IsAny<DateTime?>())).Returns(new[] { _instant.Object.NowInstant });

            _timeZone = TimeZoneInfo.Local;

            _instantFactory.Setup(x => x.GetInstant(It.IsNotNull<CrontabSchedule>(), It.IsNotNull<TimeZoneInfo>()))
                .Returns(() => _instant.Object);

            _recurringJob = new Dictionary<string, string>
            {
                { "Cron", "* * * * *" },
                { "Job", JobHelper.ToJson(InvocationData.Serialize(Job.FromExpression(() => Console.WriteLine()))) },
                { "TimeZoneId", _timeZone.Id }
            };

            _connection = new Mock<IStorageConnection>();
            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _connection.Setup(x => x.GetAllItemsFromSet("recurring-jobs"))
                .Returns(new HashSet<string> { RecurringJobId });

            _connection.Setup(x => x.GetAllEntriesFromHash(String.Format("recurring-job:{0}", RecurringJobId)))
                .Returns(_recurringJob);

            _client.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-id");
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new RecurringJobScheduler(null, _client.Object, _instantFactory.Object, _throttler.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new RecurringJobScheduler(_storage.Object, null, _instantFactory.Object, _throttler.Object));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInstantFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new RecurringJobScheduler(_storage.Object, _client.Object, null, _throttler.Object));

            Assert.Equal("instantFactory", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenThrottlerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new RecurringJobScheduler(_storage.Object, _client.Object, _instantFactory.Object, null));

            Assert.Equal("throttler", exception.ParamName);
        }

        [Fact]
        public void Execute_EnqueuesAJob_WhenItIsTimeToRunIt()
        {
            var scheduler = CreateScheduler();

            scheduler.Execute(_token);

            _client.Verify(x => x.Create(It.IsNotNull<Job>(), It.IsAny<EnqueuedState>()));
        }

        [Fact]
        public void Execute_UpdatesRecurringJobParameters_OnCompletion()
        {
            // Arrange
            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_token);

            // Assert
            var jobKey = String.Format("recurring-job:{0}", RecurringJobId);

            _connection.Verify(x => x.SetRangeInHash(
                jobKey,
                It.Is<Dictionary<string, string>>(rj =>
                    rj.ContainsKey("LastJobId") && rj["LastJobId"] == "job-id")));

            _connection.Verify(x => x.SetRangeInHash(
                jobKey,
                It.Is<Dictionary<string, string>>(rj =>
                    rj.ContainsKey("LastExecution") && rj["LastExecution"]
                        == JobHelper.SerializeDateTime(_instant.Object.NowInstant))));

            _connection.Verify(x => x.SetRangeInHash(
                jobKey,
                It.Is<Dictionary<string, string>>(rj =>
                    rj.ContainsKey("NextExecution") && rj["NextExecution"]
                        == JobHelper.SerializeDateTime(_instant.Object.NowInstant))));
        }

        [Fact]
        public void Execute_DoesNotEnqueueRecurringJob_AndDoesNotUpdateIt_ButNextExecution_WhenItIsNotATimeToRunIt()
        {
            _instant.Setup(x => x.GetNextInstants(It.IsAny<DateTime?>())).Returns(Enumerable.Empty<DateTime>);
            var scheduler = CreateScheduler();

            scheduler.Execute(_token);

            _client.Verify(
                x => x.Create(It.IsAny<Job>(), It.IsAny<EnqueuedState>()),
                Times.Never);

            _connection.Verify(x => x.SetRangeInHash(
                String.Format("recurring-job:{0}", RecurringJobId),
                It.Is<Dictionary<string, string>>(rj =>
                    rj.ContainsKey("NextExecution") && rj["NextExecution"]
                        == JobHelper.SerializeDateTime(_instant.Object.NextInstant))));
        }

        [Fact]
        public void Execute_TakesIntoConsideration_LastExecutionTime_ConvertedToLocalTimezone()
        {
            var time = DateTime.UtcNow;
            _recurringJob["LastExecution"] = JobHelper.SerializeDateTime(time);
            var scheduler = CreateScheduler();

            scheduler.Execute(_token);

            _instant.Verify(x => x.GetNextInstants(time));
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
        public void Execute_HandlesJobLoadException()
        {
            // Arrange
            _recurringJob["Job"] =
                JobHelper.ToJson(new InvocationData("SomeType", "SomeMethod", "Parameters", "arguments"));

            var scheduler = CreateScheduler();

            // Act & Assert
            Assert.DoesNotThrow(() => scheduler.Execute(_token));
        }

        [Fact]
        public void Execute_GetsInstance_InAGivenTimeZone()
        {
            // Arrange
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Hawaiian Standard Time");
            _recurringJob["TimeZoneId"] = timeZone.Id;

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_token);

            // Assert
            _instantFactory.Verify(x => x.GetInstant(It.IsAny<CrontabSchedule>(), timeZone));
        }

        [Fact]
        public void Execute_GetInstance_UseUtcTimeZone_WhenItIsNotProvided()
        {
            _recurringJob.Remove("TimeZoneId");
            var scheduler = CreateScheduler();

            scheduler.Execute(_token);

            _instantFactory.Verify(x => x.GetInstant(It.IsAny<CrontabSchedule>(), TimeZoneInfo.Utc));
        }

        [Fact]
        public void Execute_GetInstance_DoesNotCreateAJob_WhenGivenOneIsNotFound()
        {
            _recurringJob["TimeZoneId"] = "Some garbage";
            var scheduler = CreateScheduler();

            scheduler.Execute(_token);

            _client.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
        }

        private RecurringJobScheduler CreateScheduler()
        {
            return new RecurringJobScheduler(
                _storage.Object, 
                _client.Object, 
                _instantFactory.Object,
                _throttler.Object);
        }
    }
}
