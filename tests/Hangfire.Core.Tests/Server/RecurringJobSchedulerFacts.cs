extern alias ReferencedCronos;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ReferencedCronos::Cronos;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class RecurringJobSchedulerFacts
    {
        private const string RecurringJobId = "recurring-job-id";

        private readonly Mock<JobStorageConnection> _connection;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly Dictionary<string, string> _recurringJob;
        private readonly Func<DateTime> _nowInstantFactory;
        private readonly Mock<ITimeZoneResolver> _timeZoneResolver;
        private readonly BackgroundProcessContextMock _context;
        private readonly Mock<IBackgroundJobFactory> _factory;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly BackgroundJobMock _backgroundJobMock;

        private static readonly string _expressionString = "* * * * *";
        private static readonly TimeSpan _delay = TimeSpan.FromTicks(1);
        private readonly CronExpression _cronExpression = CronExpression.Parse(_expressionString);
        private readonly DateTime _nowInstant = new DateTime(2017, 03, 30, 15, 30, 0, DateTimeKind.Utc);
        private readonly DateTime _nextInstant;
        private readonly List<string> _schedule = new List<string> { RecurringJobId };

        public RecurringJobSchedulerFacts()
        {
            _context = new BackgroundProcessContextMock();

            // Setting up the successful path

            var timeZone = TimeZoneInfo.Local;

            _nowInstantFactory = () => _nowInstant;

            _timeZoneResolver = new Mock<ITimeZoneResolver>();
            _timeZoneResolver.Setup(x => x.GetTimeZoneById(It.IsAny<string>())).Throws<InvalidTimeZoneException>();
            _timeZoneResolver.Setup(x => x.GetTimeZoneById(timeZone.Id)).Returns(timeZone);
            _timeZoneResolver.Setup(x => x.GetTimeZoneById("UTC")).Returns(TimeZoneInfo.Utc);

            // ReSharper disable once PossibleInvalidOperationException
            _nextInstant = _cronExpression.GetNextOccurrence(_nowInstant, timeZone).Value;

            _recurringJob = new Dictionary<string, string>
            {
                { "Cron", _expressionString },
                { "Job", InvocationData.SerializeJob(Job.FromExpression(() => Console.WriteLine())).SerializePayload() },
                { "TimeZoneId", timeZone.Id }
            };

            _connection = new Mock<JobStorageConnection>();
            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _connection.Setup(x => x.GetFirstByLowestScoreFromSet("recurring-jobs", 0, JobHelper.ToTimestamp(_nowInstant)))
                .Returns(() => _schedule.FirstOrDefault());

            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{RecurringJobId}"))
                .Returns(_recurringJob);

            _connection.SetupSequence(x => x.GetFirstByLowestScoreFromSet("recurring-jobs", 0, JobHelper.ToTimestamp(_nowInstant), It.IsAny<int>()))
                .Returns(() => _schedule.ToList());

            _transaction = new Mock<IWriteOnlyTransaction>();
            _transaction
                .Setup(x => x.RemoveFromSet("recurring-jobs", It.IsNotNull<string>()))
                .Callback<string, string>((key, value) => _schedule.Remove(value));
            _transaction
                .Setup(x => x.AddToSet("recurring-jobs", It.IsNotNull<string>(), It.IsAny<double>()))
                .Callback<string, string, double>((key, value, score) => _schedule.Remove(value));

            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            _backgroundJobMock = new BackgroundJobMock();

            _factory = new Mock<IBackgroundJobFactory>();
            _factory.Setup(x => x.Create(It.IsAny<CreateContext>())).Returns(_backgroundJobMock.Object);
            
            _stateMachine = new Mock<IStateMachine>();
            _factory.SetupGet(x => x.StateMachine).Returns(_stateMachine.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new RecurringJobScheduler(null, _delay, _timeZoneResolver.Object, _nowInstantFactory));

            Assert.Equal("factory", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTimeZoneResolverIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                // ReSharper disable once AssignNullToNotNullAttribute
                () => new RecurringJobScheduler(_factory.Object, _delay, null, _nowInstantFactory));

            Assert.Equal("timeZoneResolver", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenNowInstantFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                // ReSharper disable once AssignNullToNotNullAttribute
                () => new RecurringJobScheduler(_factory.Object, _delay, _timeZoneResolver.Object, null));

            Assert.Equal("nowFactory", exception.ParamName);
        }

        [Fact]
        public void Execute_ThrowsAnException_WhenContextIsNull()
        {
            var scheduler = CreateScheduler();

            // ReSharper disable once AssignNullToNotNullAttribute
            var exception = Assert.Throws<ArgumentNullException>(() => scheduler.Execute(null));

            Assert.Equal("context", exception.ParamName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_EnqueuesAJob_WhenItIsTimeToRunIt(bool useJobStorageConnection)
        {
            SetupConnection(useJobStorageConnection);
            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _factory.Verify(x => x.Create(It.IsNotNull<CreateContext>()));
            _stateMachine.Verify(x => x.ApplyState(It.Is<ApplyStateContext>(ctx => ctx.NewState is EnqueuedState)));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_DoesNotHandleRecurringJobs_CreatedByNewerVersion(bool useJobStorageConnection)
        {
            SetupConnection(useJobStorageConnection);
            _recurringJob["V"] = "3";
            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_ReschedulesRecurringJobs_WithUnsupportedVersions_WhenSomeRetriesLeft(bool batching)
        {
            // Arrange
            SetupConnection(batching);
            _recurringJob["V"] = "3";
            var scheduler = CreateScheduler();
            
            // Act
            scheduler.Execute(_context.Object);
            
            // Assert
            _transaction.Verify(x => x.SetRangeInHash(It.IsAny<string>(), It.Is<Dictionary<string, string>>(dict =>
                dict["RetryAttempt"] == "1")));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, JobHelper.ToTimestamp(_nowInstant + _delay)));
            
            _transaction.Verify(x => x.Commit());
        }
        
        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_DisablesRecurringJobs_WithUnsupportedVersions_WhenRetryAttemptsExceeded(bool batching)
        {
            // Arrange
            SetupConnection(batching);
            _recurringJob["V"] = "3";
            _recurringJob["RetryAttempt"] = "10";
            var scheduler = CreateScheduler();
            
            // Act
            scheduler.Execute(_context.Object);
            
            // Assert
            _transaction.Verify(x => x.SetRangeInHash(It.IsAny<string>(), It.Is<Dictionary<string, string>>(dict =>
                dict["Error"].Contains("supported version"))));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, -1));
            
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_EnqueuesAJobToAGivenQueue_WhenItIsTimeToRunIt(bool useJobStorageConnection)
        {
            SetupConnection(useJobStorageConnection);
            _recurringJob["Queue"] = "critical";
            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _stateMachine.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(ctx => ((EnqueuedState)ctx.NewState).Queue == "critical")));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_UpdatesRecurringJobParameters_OnCompletion(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            var jobKey = $"recurring-job:{RecurringJobId}";

            _transaction.Verify(x => x.SetRangeInHash(
                jobKey,
                It.Is<Dictionary<string, string>>(rj =>
                    rj.ContainsKey("LastJobId") && rj["LastJobId"] == _backgroundJobMock.Id)));

            _transaction.Verify(x => x.SetRangeInHash(
                jobKey,
                It.Is<Dictionary<string, string>>(rj =>
                    rj.ContainsKey("LastExecution") && rj["LastExecution"]
                        == JobHelper.SerializeDateTime(_nowInstant))));

            _transaction.Verify(x => x.SetRangeInHash(
                jobKey,
                It.Is<Dictionary<string, string>>(rj =>
                    rj.ContainsKey("NextExecution") && rj["NextExecution"]
                        == JobHelper.SerializeDateTime(_nextInstant))));
            
            _transaction.Verify(x => x.SetRangeInHash(
                jobKey,
                It.Is<Dictionary<string, string>>(rj => !rj.ContainsKey("RetryAttempt"))));
            
            _transaction.Verify(x => x.Commit());
        }
        
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_DoesNotUpdateRetryAttempt_WhenItWasNotModified(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            _recurringJob["LastExecution"] = JobHelper.SerializeDateTime(_nowInstant);
            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(_nowInstant);
            _recurringJob["V"] = "2";
            _recurringJob["RetryAttempt"] = "0";
            
            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            
            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
            
            _transaction.Verify(x => x.SetRangeInHash(
                $"recurring-job:{RecurringJobId}",
                It.Is<Dictionary<string, string>>(rj => !rj.ContainsKey("RetryAttempt"))));
            
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, JobHelper.ToTimestamp(_nowInstant.AddMinutes(1))));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_DoesNotEnqueueRecurringJob_AndDoesNotUpdateIt_ButNextExecution_WhenItIsNotATimeToRunIt(
            bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            var scheduler = CreateScheduler(_nowInstant);

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
            _stateMachine.Verify(x => x.ApplyState(It.IsAny<ApplyStateContext>()), Times.Never);

            _transaction.Verify(x => x.SetRangeInHash(
                $"recurring-job:{RecurringJobId}",
                It.Is<Dictionary<string, string>>(rj =>
                    rj.ContainsKey("NextExecution") && rj["NextExecution"]
                        == JobHelper.SerializeDateTime(_nextInstant))));
            
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_TakesIntoConsideration_LastExecutionTime_ConvertedToLocalTimezone(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            var time = _nowInstant;
            _recurringJob["LastExecution"] = JobHelper.SerializeDateTime(time);

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
            _stateMachine.Verify(x => x.ApplyState(It.IsAny<ApplyStateContext>()), Times.Never);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_RemovesRecurringJobFromSchedule_WhenHashDoesNotExist(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            
            _schedule.Clear();
            _schedule.Add("non-existing-job");

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(x => x.RemoveFromSet("recurring-jobs", "non-existing-job"));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_HandlesJobLoadException(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            _recurringJob["Job"] = JobHelper.ToJson(new InvocationData("SomeType", "SomeMethod", "Parameters", "arguments"));

            var scheduler = CreateScheduler();

            // Act & Assert does not throw
            scheduler.Execute(_context.Object);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_GetsInstance_InAGivenTimeZone(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);

            var timeZoneId = PlatformHelper.IsRunningOnWindows() ? "Hawaiian Standard Time" : "Pacific/Honolulu";
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            _recurringJob["TimeZoneId"] = timeZone.Id;
            var scheduler = CreateScheduler();

            // Act & Assert does not throw
            scheduler.Execute(_context.Object);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_GetInstance_DoesNotCreateAJob_WhenGivenOneIsNotFound(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            _recurringJob["TimeZoneId"] = "Some garbage";
            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_UsesGivenCreatedAtTime(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            var createdAt = _nowInstant.AddHours(-3);
            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(createdAt);

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_DoesNotFixCreatedAtField_IfItExists(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(DateTime.UtcNow);
            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);
            
            // Assert
            _connection.Verify(
                x => x.SetRangeInHash(
                    $"recurring-job:{RecurringJobId}",
                    It.Is<Dictionary<string, string>>(rj => rj.ContainsKey("CreatedAt"))),
                Times.Never);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_FixedMissingCreatedAtField(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            _recurringJob.Remove("CreatedAt");
            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(
                x => x.SetRangeInHash(
                    $"recurring-job:{RecurringJobId}",
                    It.Is<Dictionary<string, string>>(rj => rj.ContainsKey("CreatedAt"))),
                Times.Once);
            
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_UsesNextExecutionTime_WhenBothLastExecutionAndCreatedAtAreNotAvailable(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            var nextExecution = _nowInstant.AddHours(-10);
            _recurringJob["NextExecution"] = JobHelper.SerializeDateTime(nextExecution);
            _recurringJob.Remove("CreatedAt");
            _recurringJob.Remove("LastExecution");

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(x => x.SetRangeInHash(
                $"recurring-job:{RecurringJobId}",
                It.Is<Dictionary<string, string>>(rj =>
                    rj.ContainsKey("LastExecution") && rj["LastExecution"]
                    == JobHelper.SerializeDateTime(_nowInstant))));
            
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_DoesNotThrowDistributedLockTimeoutException(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            _connection
                .Setup(x => x.AcquireDistributedLock("recurring-jobs:lock", It.IsAny<TimeSpan>()))
                .Throws(new DistributedLockTimeoutException("recurring-jobs:lock"));

            var scheduler = CreateScheduler();

            // Act & Assert (Does Not Throw)
            scheduler.Execute(_context.Object);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_DoesNotEnqueueRecurringJob_WhenItIsCorrectAndItWasNotTriggered(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);

            _recurringJob["NextExecution"] = JobHelper.SerializeDateTime(_nowInstant.AddMinutes(1));
            _recurringJob["LastExecution"] = JobHelper.SerializeDateTime(_nowInstant);
            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(_nowInstant);

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);
            
            // Assert
            _stateMachine.Verify(x => x.ApplyState(It.IsAny<ApplyStateContext>()), Times.Never);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_AcquiresDistributedLock_ForEachRecurringJob(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _connection.Verify(x => x.AcquireDistributedLock("lock:recurring-job:recurring-job-id", It.IsAny<TimeSpan>()));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_SchedulesNextExecution_AfterCreatingAJob(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _stateMachine.Verify(x => x.ApplyState(It.IsAny<ApplyStateContext>()));

            _transaction.Verify(x => x.SetRangeInHash(
                $"recurring-job:{RecurringJobId}",
                It.Is<Dictionary<string, string>>(rj =>
                    rj.ContainsKey("NextExecution") && 
                    rj["NextExecution"] == JobHelper.SerializeDateTime(_nowInstant.AddMinutes(1)))));

            _transaction.Verify(x => x.AddToSet(
                "recurring-jobs", 
                "recurring-job-id", 
                JobHelper.ToTimestamp(_nowInstant.AddMinutes(1))));

            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_FixesNextExecution_WhenItsNotATimeToRunAJob(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            _recurringJob["LastExecution"] = JobHelper.SerializeDateTime(_nowInstant);
            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _stateMachine.Verify(x => x.ApplyState(It.IsAny<ApplyStateContext>()), Times.Never);

            _transaction.Verify(x => x.SetRangeInHash(
                $"recurring-job:{RecurringJobId}",
                It.Is<Dictionary<string, string>>(rj =>
                    rj.ContainsKey("NextExecution") &&
                    rj["NextExecution"] == JobHelper.SerializeDateTime(_nowInstant.AddMinutes(1)))));

            _transaction.Verify(x => x.AddToSet(
                "recurring-jobs",
                "recurring-job-id",
                JobHelper.ToTimestamp(_nowInstant.AddMinutes(1))));

            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Execute_DoesNotCycleImmediately_WhenItCantDeserializeEverything(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);

            _factory.Setup(x => x.Create(It.IsAny<CreateContext>())).Throws<InvalidOperationException>();

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _connection.Verify(x => x.GetAllEntriesFromHash(It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_UsesTimeZoneResolver_WhenCalculatingNextExecution(bool batching)
        {
            // Arrange
            SetupConnection(batching);

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(PlatformHelper.IsRunningOnWindows()
                ? "Hawaiian Standard Time"
                : "Pacific/Honolulu");

            _timeZoneResolver
                .Setup(x => x.GetTimeZoneById(It.Is<string>(id => id == "Hawaiian Standard Time" || id == "Pacific/Honolulu")))
                .Returns(timeZone);

            // We are returning IANA time zone on Windows and Windows time zone on Linux.
            _recurringJob["Cron"] = "0 0 * * *";
            _recurringJob["TimeZoneId"] = PlatformHelper.IsRunningOnWindows() ? "Pacific/Honolulu" : "Hawaiian Standard Time";
            _recurringJob["NextExecution"] = JobHelper.SerializeDateTime(_nowInstant.AddHours(18).AddMinutes(30));

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(x => x.SetRangeInHash($"recurring-job:{RecurringJobId}", It.Is<Dictionary<string, string>>(dict =>
                !dict.ContainsKey("NextExecution"))));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, JobHelper.ToTimestamp(_nowInstant.AddHours(18).AddMinutes(30))));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_DoesNotScheduleRecurringJob_ToThePast(bool batching)
        {
            // Arrange
            SetupConnection(batching);

            _recurringJob["LastExecution"] = JobHelper.SerializeDateTime(_nowInstant.AddMinutes(-2));

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(x => x.SetRangeInHash($"recurring-job:{RecurringJobId}", It.Is<Dictionary<string, string>>(dict =>
                dict["NextExecution"] == JobHelper.SerializeDateTime(_nowInstant.AddMinutes(1)))));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, JobHelper.ToTimestamp(_nowInstant.AddMinutes(1))));
            _transaction.Verify(x => x.Commit(), Times.Once);
        }

        [Fact]
        public void Execute_DoesNotUseBatchedMethod_WhenStorageConnectionThrowsAnException()
        {
            // Arrange
            SetupConnection(true);
            _connection
                .Setup(x => x.GetFirstByLowestScoreFromSet(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>()))
                .Throws<NotSupportedException>();

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, JobHelper.ToTimestamp(_nowInstant.AddMinutes(1))));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_AlwaysUpdatesScoreForTheSetItem_EvenIfRecurringJobWasNotChanged(bool batching)
        {
            // Arrange
            SetupConnection(batching);

            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(_nowInstant.AddMinutes(-1));
            _recurringJob["LastExecution"] = JobHelper.SerializeDateTime(_nowInstant);
            _recurringJob["NextExecution"] = JobHelper.SerializeDateTime(_nowInstant.AddMinutes(1));
            _recurringJob["V"] = "2";

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
            _transaction.Verify(x => x.SetRangeInHash(It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()), Times.Never);
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, JobHelper.ToTimestamp(_nowInstant.AddMinutes(1))));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_UsesUtcTimeZone_WhenCorrespondingFieldIsNullOrEmpty(bool batching)
        {
            // Arrange
            SetupConnection(batching);

            _recurringJob["TimeZoneId"] = null;
            _recurringJob["Cron"] = "0 30 15 30 03 *";

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()));
        }
        
        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_ReschedulesRecurringJob_WhenCronExpressionIsInvalid_AndRetryAttemptsAreNotExceeded(bool batching)
        {
            // Arrange
            SetupConnection(batching);

            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(_nowInstant.AddDays(-1));
            _recurringJob["Cron"] = "some garbage";
            _recurringJob["V"] = "2";

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            Assert.True(_delay > TimeSpan.Zero);
            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
            _transaction.Verify(x => x.SetRangeInHash(It.IsAny<string>(), It.Is<Dictionary<string, string>>(dict =>
                dict.Count == 3 && dict["RetryAttempt"] == "1" && dict.ContainsKey("Error"))));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, JobHelper.ToTimestamp(_nowInstant.Add(_delay))));
            _transaction.Verify(x => x.Commit());
        }
        
        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_DoesNotFailOnInvalidCronExpression_AndSimplySetsNextExecutionToNull_WhenRetryAttemptsExceeded(bool batching)
        {
            // Arrange
            SetupConnection(batching);

            _recurringJob["Cron"] = "some garbage";
            _recurringJob["RetryAttempt"] = "10";

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, -1));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_ClearsLastError_AndRetryAttempts_AfterSuccessfulScheduling(bool batching)
        {
            // Arrange
            SetupConnection(batching);

            var scheduler = CreateScheduler();

            _recurringJob["Error"] = "Some error that previously happened";
            _recurringJob["RetryAttempt"] = "10";

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _factory.Verify(x => x.Create(It.IsNotNull<CreateContext>()));
            _transaction.Verify(x => x.SetRangeInHash(It.IsAny<string>(), It.Is<Dictionary<string, string>>(dict =>
                dict["Error"] == String.Empty && dict["RetryAttempt"] == "0")));
            _transaction.Verify(x => x.Commit());
        }
        
        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_ReschedulesRecurringJob_WhenThereAreIssuesWithJobLoading_AndRetryAttemptsAreNotExceeded(bool batching)
        {
            // Arrange
            SetupConnection(batching);

            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(_nowInstant.AddDays(-1));
            _recurringJob["Job"] = null;
            _recurringJob["V"] = "2";

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);
            
            // Assert
            Assert.True(_delay > TimeSpan.Zero);
            
            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
            
            _transaction.Verify(x => x.SetRangeInHash(It.IsAny<string>(), It.Is<Dictionary<string, string>>(dict =>
                dict.Count == 3 && dict["RetryAttempt"] == "1" && dict.ContainsKey("Error"))));
            
            _transaction.Verify(x => x.AddToSet(
                "recurring-jobs",
                RecurringJobId,
                JobHelper.ToTimestamp(_nowInstant.Add(_delay))));
            
            _transaction.Verify(x => x.Commit());
        }
        
        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_ReschedulesRecurringJob_WithIncreasedAttemptNumber_WhenThereAreIssuesWithJobLoading_AndRetryAttemptsAreNotExceeded(bool batching)
        {
            // Arrange
            SetupConnection(batching);

            _recurringJob["Job"] = null;
            _recurringJob["RetryAttempt"] = "1";

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);
            
            // Assert
            _transaction.Verify(x => x.SetRangeInHash(It.IsAny<string>(), It.Is<Dictionary<string, string>>(dict =>
                dict.ContainsKey("RetryAttempt") && dict["RetryAttempt"] == "2")));
            
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_HidesRecurringJob_FromScheduler_WhenJobCanNotBeLoaded_AndRetryAttemptsExceeded(bool batching)
        {
            // Arrange
            SetupConnection(batching);

            _recurringJob["RetryAttempt"] = "10";
            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(_nowInstant.AddDays(-1));
            _recurringJob["NextExecution"] = JobHelper.SerializeDateTime(_nowInstant);
            _recurringJob["Job"] = InvocationData.SerializeJob(
                Job.FromExpression(() => Console.WriteLine())).SerializePayload().Replace("Console", "SomeNonExistingClass");

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(x => x.SetRangeInHash(It.IsAny<string>(), It.Is<Dictionary<string, string>>(dict => 
                dict.Count == 3 &&
                dict["NextExecution"] == String.Empty &&
                dict["Error"].Contains("JobLoadException") &&
                dict["V"] == "2")));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, -1));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_HidesRecurringJob_FromScheduler_WhenJobCanNotBeDeserialized_AndRetryAttemptsExceeded(bool batching)
        {
            // Arrange
            SetupConnection(batching);

            _recurringJob["RetryAttempt"] = "10";
            _recurringJob["Job"] = "Some garbage";
            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(_nowInstant.AddDays(-1));
            _recurringJob["NextExecution"] = JobHelper.SerializeDateTime(_nowInstant);

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(x => x.SetRangeInHash(It.IsAny<string>(), It.Is<Dictionary<string, string>>(dict => 
                dict.Count == 3 &&
                dict["NextExecution"] == String.Empty &&
                dict["Error"].Contains("JsonReaderException") &&
                dict["V"] == "2")));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, -1));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_HidesRecurringJob_FromScheduler_WhenJobIsNull_AndRetryAttemptsExceeded(bool batching)
        {
            // Arrange
            SetupConnection(batching);

            _recurringJob["RetryAttempt"] = "10";
            _recurringJob["Job"] = null;
            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(_nowInstant.AddDays(-1));
            _recurringJob["NextExecution"] = JobHelper.SerializeDateTime(_nowInstant);

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(x => x.SetRangeInHash(It.IsAny<string>(), It.Is<Dictionary<string, string>>(dict => 
                dict.Count == 3 &&
                dict["NextExecution"] == String.Empty &&
                dict["Error"].Contains("The 'Job' field has a null") &&
                dict["V"] == "2")));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, -1));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_HidesRecurringJob_FromScheduler_WhenTimeZoneCanNotBeResolved_AndRetryAttemptsExceeded(bool batching)
        {
            // Arrange
            SetupConnection(batching);

            _recurringJob["RetryAttempt"] = "10";
            _recurringJob["TimeZoneId"] = "Non-existing time zone";
            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(_nowInstant.AddDays(-1));
            _recurringJob["NextExecution"] = JobHelper.SerializeDateTime(_nowInstant);

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(x => x.SetRangeInHash(It.IsAny<string>(), It.Is<Dictionary<string, string>>(dict => 
                dict.Count == 3 &&
                dict["NextExecution"] == String.Empty &&
                dict["Error"].Contains("System.InvalidTimeZoneException") &&
                dict["V"] == "2")));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, -1));
            _transaction.Verify(x => x.Commit());
        }
        
        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_ReschedulesRecurringJob_WhenFactoryThrowsAnException_AndRetryAttemptsAreNotExceeded(bool batching)
        {
            // Arrange
            SetupConnection(batching);
            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(_nowInstant.AddDays(-1));
            _recurringJob["V"] = "2";
            _factory.Setup(x => x.Create(It.IsAny<CreateContext>())).Throws<InvalidOperationException>();

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);
            
            // Assert
            Assert.True(_delay > TimeSpan.Zero);
            
            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Once);
            
            _transaction.Verify(x => x.SetRangeInHash(It.IsAny<string>(), It.Is<Dictionary<string, string>>(dict =>
                dict.Count == 3 && dict["RetryAttempt"] == "1" && dict.ContainsKey("Error"))));
            
            _transaction.Verify(x => x.AddToSet(
                "recurring-jobs",
                RecurringJobId,
                JobHelper.ToTimestamp(_nowInstant.Add(_delay))));
            
            _transaction.Verify(x => x.Commit());
        }
        
        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_HidesRecurringJob_FromScheduler_WhenFactoryThrowsAnException_AndRetryAttemptsExceeded(bool batching)
        {
            // Arrange
            SetupConnection(batching);
            _factory.Setup(x => x.Create(It.IsAny<CreateContext>())).Throws(new InvalidOperationException("Invalid operation"));
            _recurringJob["RetryAttempt"] = "10";
            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(_nowInstant.AddDays(-1));
            _recurringJob["NextExecution"] = JobHelper.SerializeDateTime(_nowInstant);

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            // Assert
            _transaction.Verify(x => x.SetRangeInHash(It.IsAny<string>(), It.Is<Dictionary<string, string>>(dict => 
                dict.Count == 3 &&
                dict["NextExecution"] == String.Empty &&
                dict["Error"].StartsWith("System.InvalidOperationException") &&
                dict["V"] == "2")));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, -1));
            _transaction.Verify(x => x.Commit());
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_AbleToProcessFurtherJobs_WhenStateChangerThrowsAnException_ForPreviousOnes(bool batching)
        {
            // Arrange
            SetupConnection(batching);
            _schedule.Add("AnotherId");
            _connection.Setup(x => x.GetAllEntriesFromHash("recurring-job:AnotherId"))
                .Returns(_recurringJob);

            _factory
                .Setup(x => x.Create(It.Is<CreateContext>(ctx => (string)ctx.Parameters["RecurringJobId"] == RecurringJobId)))
                .Throws<InvalidOperationException>();

            var scheduler = CreateScheduler();
            
            // Act
            scheduler.Execute(_context.Object);
            
            // Assert
            _factory.Verify(
                x => x.Create(It.Is<CreateContext>(ctx => (string)ctx.Parameters["RecurringJobId"] == "AnotherId")),
                Times.Once);
        }

        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_RemovesNonExistingRecurringJobFromSet_AndDoesNotStopPipelineImmediatelyInThisCase(bool batching)
        {
            // Arrange
            SetupConnection(batching);
            _schedule.Add("AnotherId");
            _connection.Setup(x => x.GetAllEntriesFromHash("recurring-job:AnotherId")).Returns(_recurringJob);
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{RecurringJobId}")).Returns<Dictionary<string, string>>(null);

            var scheduler = CreateScheduler();
            
            // Act
            scheduler.Execute(_context.Object);
            
            // Assert
            _transaction.Verify(x => x.RemoveFromSet("recurring-jobs", RecurringJobId));
            _factory.Verify(
                x => x.Create(It.Is<CreateContext>(ctx => (string)ctx.Parameters["RecurringJobId"] == "AnotherId")),
                Times.Once);
        }
        
        [Theory]
        [InlineData(false), InlineData(true)]
        public void Execute_DoesNotRescheduleRecurringJob_WhenExceptionRaisedFromTransactionCommit(bool batching)
        {
            // Arrange
            SetupConnection(batching);
            _recurringJob["RetryAttempt"] = "10";
            _transaction.SetupSequence(x => x.Commit())
                .Throws<InvalidOperationException>()
                .Pass();

            var scheduler = CreateScheduler();
            
            // Act
            Assert.Throws<InvalidOperationException>(() => scheduler.Execute(_context.Object));
            
            // Assert
            _transaction.Verify(
                x => x.SetRangeInHash(It.IsAny<string>(), It.Is<Dictionary<string, string>>(dict => 
                    dict.ContainsKey("Error") &&
                    dict["NextExecution"] == String.Empty)),
                Times.Never);
            _transaction.Verify(x => x.AddToSet("recurring-jobs", RecurringJobId, -1), Times.Never);
            _transaction.Verify(x => x.Commit(), Times.Once);
        }

        private void SetupConnection(bool useJobStorageConnection)
        {
            if (useJobStorageConnection) EnableBatching();
        }
        
        private void EnableBatching()
        {
            _connection
                .Setup(x => x.GetFirstByLowestScoreFromSet(null, It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>()))
                .Throws(new ArgumentNullException("key"));
        }

        private RecurringJobScheduler CreateScheduler(DateTime? lastExecution = null)
        {
            var scheduler = new RecurringJobScheduler(
                _factory.Object,
                _delay,
                _timeZoneResolver.Object,
                _nowInstantFactory);

            if (lastExecution.HasValue)
            {
                _recurringJob.Add("LastExecution", JobHelper.SerializeDateTime(lastExecution.Value));
            }

            return scheduler;
        }
    }
}
