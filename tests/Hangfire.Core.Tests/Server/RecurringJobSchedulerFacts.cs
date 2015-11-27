using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Client;
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

        private readonly Mock<IStorageConnection> _connection;
        private readonly Dictionary<string, string> _recurringJob;
        private Func<CrontabSchedule, TimeZoneInfo, IScheduleInstant> _instantFactory; 
        private readonly Mock<IThrottler> _throttler;
        private readonly Mock<IScheduleInstant> _instant;
        private readonly BackgroundProcessContextMock _context;
        private readonly Mock<IBackgroundJobFactory> _factory;
        private readonly BackgroundJobMock _backgroundJobMock;

        public RecurringJobSchedulerFacts()
        {
            _context = new BackgroundProcessContextMock();

            _throttler = new Mock<IThrottler>();

            // Setting up the successful path
            _instant = new Mock<IScheduleInstant>();
            _instant.Setup(x => x.GetNextInstants(It.IsAny<DateTime?>())).Returns(new[] { _instant.Object.NowInstant });

            var timeZone1 = TimeZoneInfo.Local;

            _instantFactory = (schedule, timeZone) => _instant.Object;

            _recurringJob = new Dictionary<string, string>
            {
                { "Cron", "* * * * *" },
                { "Job", JobHelper.ToJson(InvocationData.Serialize(Job.FromExpression(() => Console.WriteLine()))) },
                { "TimeZoneId", timeZone1.Id }
            };

            _connection = new Mock<IStorageConnection>();
            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _connection.Setup(x => x.GetAllItemsFromSet("recurring-jobs"))
                .Returns(new HashSet<string> { RecurringJobId });

            _connection.Setup(x => x.GetAllEntriesFromHash(String.Format("recurring-job:{0}", RecurringJobId)))
                .Returns(_recurringJob);

            _backgroundJobMock = new BackgroundJobMock();

            _factory = new Mock<IBackgroundJobFactory>();
            _factory.Setup(x => x.Create(It.IsAny<CreateContext>())).Returns(_backgroundJobMock.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new RecurringJobScheduler(null, _instantFactory, _throttler.Object));

            Assert.Equal("factory", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenInstantFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new RecurringJobScheduler(_factory.Object, null, _throttler.Object));

            Assert.Equal("instantFactory", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenThrottlerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new RecurringJobScheduler(_factory.Object, _instantFactory, null));

            Assert.Equal("throttler", exception.ParamName);
        }

        [Fact]
        public void Execute_EnqueuesAJob_WhenItIsTimeToRunIt()
        {
            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _factory.Verify(x => x.Create(It.IsNotNull<CreateContext>()));
        }

        [Fact]
        public void Execute_EnqueuesAJobToAGivenQueue_WhenItIsTimeToRunIt()
        {
            _recurringJob["Queue"] = "critical";
            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _factory.Verify(x => x.Create(
                It.Is<CreateContext>(cc => ((EnqueuedState)cc.InitialState).Queue == "critical")));
        }

        [Fact]
        public void Execute_UpdatesRecurringJobParameters_OnCompletion()
        {
            // Arrange
            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            var jobKey = String.Format("recurring-job:{0}", RecurringJobId);

            _connection.Verify(x => x.SetRangeInHash(
                jobKey,
                It.Is<Dictionary<string, string>>(rj =>
                    rj.ContainsKey("LastJobId") && rj["LastJobId"] == _backgroundJobMock.Id)));

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

            scheduler.Execute(_context.Object);

            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);

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

            scheduler.Execute(_context.Object);

            _instant.Verify(x => x.GetNextInstants(time));
        }
        
        [Fact]
        public void Execute_DoesNotFail_WhenRecurringJobDoesNotExist()
        {
            _connection.Setup(x => x.GetAllItemsFromSet(It.IsAny<string>()))
                .Returns(new HashSet<string> { "non-existing-job" });
            var scheduler = CreateScheduler();

            Assert.DoesNotThrow(() => scheduler.Execute(_context.Object));
        }

        [Fact]
        public void Execute_HandlesJobLoadException()
        {
            // Arrange
            _recurringJob["Job"] =
                JobHelper.ToJson(new InvocationData("SomeType", "SomeMethod", "Parameters", "arguments", "SomePath"));

            var scheduler = CreateScheduler();

            // Act & Assert
            Assert.DoesNotThrow(() => scheduler.Execute(_context.Object));
        }

        [Fact]
        public void Execute_GetsInstance_InAGivenTimeZone()
        {
            var timeZoneId = Type.GetType("Mono.Runtime") != null ? "Pacific/Honolulu" : "Hawaiian Standard Time";

            _instantFactory = (schedule, timeZoneInfo) =>
            {
                if (timeZoneInfo.Id != timeZoneId) throw new InvalidOperationException("Invalid timezone");
                return _instant.Object;
            };
            // Arrange
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            _recurringJob["TimeZoneId"] = timeZone.Id;
            var scheduler = CreateScheduler();

            // Act & Assert
            Assert.DoesNotThrow(() => scheduler.Execute(_context.Object));
        }

        [Fact]
        public void Execute_GetInstance_UseUtcTimeZone_WhenItIsNotProvided()
        {
            // Arrange
            _instantFactory = (schedule, timeZoneInfo) =>
            {
                if (!timeZoneInfo.Equals(TimeZoneInfo.Utc)) throw new InvalidOperationException("Invalid timezone");
                return _instant.Object;
            };
            _recurringJob.Remove("TimeZoneId");
            var scheduler = CreateScheduler();

            // Act & Assert
            Assert.DoesNotThrow(() => scheduler.Execute(_context.Object));
        }

        [Fact]
        public void Execute_GetInstance_DoesNotCreateAJob_WhenGivenOneIsNotFound()
        {
            _recurringJob["TimeZoneId"] = "Some garbage";
            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
        }

        private RecurringJobScheduler CreateScheduler()
        {
            return new RecurringJobScheduler(
                _factory.Object,
                _instantFactory,
                _throttler.Object);
        }
    }
}
