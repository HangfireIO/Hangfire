extern alias ReferencedCronos;

using System;
using System.Collections.Generic;
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

        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly Dictionary<string, string> _recurringJob;
        private readonly Func<DateTime> _nowInstantFactory;
        private readonly BackgroundProcessContextMock _context;
        private readonly Mock<IBackgroundJobFactory> _factory;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly BackgroundJobMock _backgroundJobMock;

        private static readonly string _expressionString = "* * * * *";
        private readonly CronExpression _cronExpression = CronExpression.Parse(_expressionString);
        private readonly DateTime _nowInstant = new DateTime(2017, 03, 30, 15, 30, 0, DateTimeKind.Utc);
        private readonly DateTime _nextInstant;
        private readonly Mock<JobStorageConnection> _storageConnection;

        public RecurringJobSchedulerFacts()
        {
            _context = new BackgroundProcessContextMock();
            _context.CancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            // Setting up the successful path

            var timeZone = TimeZoneInfo.Local;

            _nowInstantFactory = () => _nowInstant;

            // ReSharper disable once PossibleInvalidOperationException
            _nextInstant = _cronExpression.GetNextOccurrence(_nowInstant, timeZone).Value;

            _recurringJob = new Dictionary<string, string>
            {
                { "Cron", _expressionString },
                { "Job", JobHelper.ToJson(InvocationData.Serialize(Job.FromExpression(() => Console.WriteLine()))) },
                { "TimeZoneId", timeZone.Id }
            };

            _connection = new Mock<IStorageConnection>();

            _connection.SetupSequence(x => x.GetFirstByLowestScoreFromSet("recurring-jobs", 0, JobHelper.ToTimestamp(_nowInstant)))
                .Returns(RecurringJobId)
                .Returns((string)null);

            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{RecurringJobId}"))
                .Returns(_recurringJob);

            _storageConnection = new Mock<JobStorageConnection>();
            _storageConnection.Setup(x => x.GetFirstByLowestScoreFromSet(null, It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>()))
                .Throws<ArgumentNullException>();
            _storageConnection.SetupSequence(x => x.GetFirstByLowestScoreFromSet("recurring-jobs", 0, JobHelper.ToTimestamp(_nowInstant), It.IsAny<int>()))
                .Returns(new List<string> { RecurringJobId })
                .Returns((List<string>)null);

            _storageConnection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{RecurringJobId}")).Returns(_recurringJob);

            _transaction = new Mock<IWriteOnlyTransaction>();

            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);
            _storageConnection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            _backgroundJobMock = new BackgroundJobMock();

            _factory = new Mock<IBackgroundJobFactory>();
            _factory.Setup(x => x.Create(It.IsAny<CreateContext>())).Returns(_backgroundJobMock.Object);
            
            _stateMachine = new Mock<IStateMachine>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
// ReSharper disable once AssignNullToNotNullAttribute
                () => new RecurringJobScheduler(null, _stateMachine.Object, _nowInstantFactory));

            Assert.Equal("factory", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                // ReSharper disable once AssignNullToNotNullAttribute
                () => new RecurringJobScheduler(_factory.Object, null, _nowInstantFactory));
            
            Assert.Equal("stateMachine", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenNowInstantFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                // ReSharper disable once AssignNullToNotNullAttribute
                () => new RecurringJobScheduler(_factory.Object, _stateMachine.Object, null));

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
        public void Execute_DoesNotFail_WhenRecurringJobDoesNotExist(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);

            if (useJobStorageConnection)
                _storageConnection.Setup(x => x.GetAllItemsFromSet(It.IsAny<string>())).Returns(new HashSet<string> { "non-existing-job" });
            else
                _connection.Setup(x => x.GetAllItemsFromSet(It.IsAny<string>())).Returns(new HashSet<string> { "non-existing-job" });

            var scheduler = CreateScheduler();

            // Act & Assert (Does not throw)
            scheduler.Execute(_context.Object);
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

            if (useJobStorageConnection)
            {
                _storageConnection
                    .Setup(x => x.AcquireDistributedLock("recurring-jobs:lock", It.IsAny<TimeSpan>()))
                    .Throws(new DistributedLockTimeoutException("recurring-jobs:lock"));
            }
            else
            {
                _connection
                    .Setup(x => x.AcquireDistributedLock("recurring-jobs:lock", It.IsAny<TimeSpan>()))
                    .Throws(new DistributedLockTimeoutException("recurring-jobs:lock"));
            }

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
            if (useJobStorageConnection)
                _storageConnection.Verify(x => x.AcquireDistributedLock("lock:recurring-job:recurring-job-id", It.IsAny<TimeSpan>()));
            else
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
        public void Execute_FixedNextExecution_WhenItsNotATimeToRunAJob(bool useJobStorageConnection)
        {
            // Arrange
            SetupConnection(useJobStorageConnection);
            _recurringJob["NextExecution"] = JobHelper.SerializeDateTime(_nowInstant.AddMinutes(1));
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

        private void SetupConnection(bool useJobStorageConnection)
        {
            if (useJobStorageConnection) _context.Storage.Setup(x => x.GetConnection()).Returns(_storageConnection.Object);
            else _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        private RecurringJobScheduler CreateScheduler(DateTime? lastExecution = null)
        {
            var scheduler = new RecurringJobScheduler(
                _factory.Object,
                _stateMachine.Object,
                _nowInstantFactory);

            if (lastExecution.HasValue)
            {
                _recurringJob.Add("LastExecution", JobHelper.SerializeDateTime(lastExecution.Value));
            }

            return scheduler;
        }
    }
}
