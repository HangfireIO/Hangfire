﻿extern alias ReferencedCronos;

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
            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _connection.SetupSequence(x => x.GetFirstByLowestScoreFromSet("recurring-jobs", 0, JobHelper.ToTimestamp(_nowInstant)))
                .Returns(RecurringJobId)
                .Returns((string)null);

            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{RecurringJobId}"))
                .Returns(_recurringJob);
            
            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

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

        [Fact]
        public void Execute_EnqueuesAJob_WhenItIsTimeToRunIt()
        {
            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _factory.Verify(x => x.Create(It.IsNotNull<CreateContext>()));
            _stateMachine.Verify(x => x.ApplyState(It.Is<ApplyStateContext>(ctx => ctx.NewState is EnqueuedState)));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void Execute_EnqueuesAJobToAGivenQueue_WhenItIsTimeToRunIt()
        {
            _recurringJob["Queue"] = "critical";
            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _stateMachine.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(ctx => ((EnqueuedState)ctx.NewState).Queue == "critical")));
        }

        [Fact]
        public void Execute_UpdatesRecurringJobParameters_OnCompletion()
        {
            // Arrange
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

        [Fact]
        public void Execute_DoesNotEnqueueRecurringJob_AndDoesNotUpdateIt_ButNextExecution_WhenItIsNotATimeToRunIt()
        {
            var scheduler = CreateScheduler(_nowInstant);

            scheduler.Execute(_context.Object);

            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
            _stateMachine.Verify(x => x.ApplyState(It.IsAny<ApplyStateContext>()), Times.Never);

            _transaction.Verify(x => x.SetRangeInHash(
                $"recurring-job:{RecurringJobId}",
                It.Is<Dictionary<string, string>>(rj =>
                    rj.ContainsKey("NextExecution") && rj["NextExecution"]
                        == JobHelper.SerializeDateTime(_nextInstant))));
            
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void Execute_TakesIntoConsideration_LastExecutionTime_ConvertedToLocalTimezone()
        {
            var time = _nowInstant;
            _recurringJob["LastExecution"] = JobHelper.SerializeDateTime(time);

            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
            _stateMachine.Verify(x => x.ApplyState(It.IsAny<ApplyStateContext>()), Times.Never);
        }
        
        [Fact]
        public void Execute_DoesNotFail_WhenRecurringJobDoesNotExist()
        {
            _connection.Setup(x => x.GetAllItemsFromSet(It.IsAny<string>()))
                .Returns(new HashSet<string> { "non-existing-job" });
            var scheduler = CreateScheduler();

            // Does not throw
            scheduler.Execute(_context.Object);
        }

        [Fact]
        public void Execute_HandlesJobLoadException()
        {
            // Arrange
            _recurringJob["Job"] =
                JobHelper.ToJson(new InvocationData("SomeType", "SomeMethod", "Parameters", "arguments"));

            var scheduler = CreateScheduler();

            // Act & Assert does not throw
            scheduler.Execute(_context.Object);
        }

        [Fact]
        public void Execute_GetsInstance_InAGivenTimeZone()
        {
            var timeZoneId = PlatformHelper.IsRunningOnWindows() ? "Hawaiian Standard Time" : "Pacific/Honolulu";

            // Arrange
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            _recurringJob["TimeZoneId"] = timeZone.Id;
            var scheduler = CreateScheduler();

            // Act & Assert does not throw
            scheduler.Execute(_context.Object);
        }

        [Fact]
        public void Execute_GetInstance_DoesNotCreateAJob_WhenGivenOneIsNotFound()
        {
            _recurringJob["TimeZoneId"] = "Some garbage";
            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);

            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
        }

        [Fact]
        public void Execute_UsesGivenCreatedAtTime()
        {
            // Arrange
            var createdAt = _nowInstant.AddHours(-3);
            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(createdAt);

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);

            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Once);
        }

        [Fact]
        public void Execute_DoesNotFixCreatedAtField_IfItExists()
        {
            // Arrange
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

        [Fact]
        public void Execute_FixedMissingCreatedAtField()
        {
            // Arrange
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

        [Fact]
        public void Execute_UsesNextExecutionTime_WhenBothLastExecutionAndCreatedAtAreNotAvailable()
        {
            // Arrange
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

        [Fact]
        public void Execute_DoesNotThrowDistributedLockTimeoutException()
        {
            _connection
                .Setup(x => x.AcquireDistributedLock("recurring-jobs:lock", It.IsAny<TimeSpan>()))
                .Throws(new DistributedLockTimeoutException("recurring-jobs:lock"));

            var scheduler = CreateScheduler();

            scheduler.Execute(_context.Object);
        }

        [Fact]
        public void Execute_DoesNotEnqueueRecurringJob_WhenItIsCorrectAndItWasNotTriggered()
        {
            // Arrange
            _recurringJob["NextExecution"] = JobHelper.SerializeDateTime(_nowInstant.AddMinutes(1));
            _recurringJob["LastExecution"] = JobHelper.SerializeDateTime(_nowInstant);
            _recurringJob["CreatedAt"] = JobHelper.SerializeDateTime(_nowInstant);

            var scheduler = CreateScheduler();

            // Act
            scheduler.Execute(_context.Object);
            
            // Assert
            _stateMachine.Verify(x => x.ApplyState(It.IsAny<ApplyStateContext>()), Times.Never);
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
