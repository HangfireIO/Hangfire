using System;
using System.Collections.Generic;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;
#pragma warning disable 618

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests
{
    public class RecurringJobManagerFacts
    {
        private readonly Mock<JobStorage> _storage;
        private readonly string _id;
        private readonly Job _job;
        private readonly string _cronExpression;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IWriteOnlyTransaction> _transaction;
        private readonly Mock<IBackgroundJobFactory> _factory;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly DateTime _now = new DateTime(2017, 03, 30, 15, 30, 0, DateTimeKind.Utc);
        private readonly Func<DateTime> _nowFactory;
        private readonly BackgroundJob _backgroundJob;
        private readonly Mock<ITimeZoneResolver> _timeZoneResolver;

        public RecurringJobManagerFacts()
        {
            _id = "recurring-job-id";
            _job = Job.FromExpression(() => Method());
            _backgroundJob = new BackgroundJob("my-id", _job, _now);
            _cronExpression = Cron.Minutely();
            _storage = new Mock<JobStorage>();
            _factory = new Mock<IBackgroundJobFactory>();
            _stateMachine = new Mock<IStateMachine>();
            _factory.SetupGet(x => x.StateMachine).Returns(_stateMachine.Object);
            _nowFactory = () => _now;

            _timeZoneResolver = new Mock<ITimeZoneResolver>();
            _timeZoneResolver.Setup(x => x.GetTimeZoneById(It.IsAny<string>()))
                .Returns<string>(TimeZoneInfo.FindSystemTimeZoneById);

            _connection = new Mock<IStorageConnection>();
            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);

            _factory.Setup(x => x.Create(It.Is<CreateContext>(ctx =>
                    ctx.Storage == _storage.Object &&
                    ctx.Connection == _connection.Object &&
                    ctx.InitialState == null)))
                .Returns(_backgroundJob);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RecurringJobManager(null, _factory.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RecurringJobManager(_storage.Object, (IBackgroundJobFactory)null));

            Assert.Equal("factory", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTimeZoneResolverIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RecurringJobManager(_storage.Object, _factory.Object, null, _nowFactory));

            Assert.Equal("timeZoneResolver", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenNowFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RecurringJobManager(_storage.Object, _factory.Object, _timeZoneResolver.Object, null));

            Assert.Equal("nowFactory", exception.ParamName);
        }

        [Fact]
        public void AddOrUpdate_ThrowsAnException_WhenIdIsNull()
        {
            var manager = CreateManager();

            var exception = Assert.Throws<ArgumentNullException>(
                () => manager.AddOrUpdate(null, _job, Cron.Daily()));

            Assert.Equal("recurringJobId", exception.ParamName);
        }

        [Fact]
        public void AddOrUpdate_ThrowsAnException_WhenJobIsNull()
        {
            var manager = CreateManager();

            var exception = Assert.Throws<ArgumentNullException>(
                () => manager.AddOrUpdate(_id, null, Cron.Daily()));

            Assert.Equal("job", exception.ParamName);
        }

        [Fact]
        public void AddOrUpdate_ThrowsAnException_WhenQueueNameIsNull()
        {
            var manager = CreateManager();

            var exception = Assert.Throws<ArgumentNullException>(
                () => manager.AddOrUpdate(_id, _job, Cron.Daily(), TimeZoneInfo.Local, null));

            Assert.Equal("queue", exception.ParamName);
        }
        
        [Fact]
        public void AddOrUpdate_ThrowsAnException_WhenCronExpressionIsNull()
        {
            var manager = CreateManager();

            var exception = Assert.Throws<ArgumentNullException>(
                () => manager.AddOrUpdate(_id, _job, null));

            Assert.Equal("cronExpression", exception.ParamName);
        }

        [Fact]
        public void AddOrUpdate_ThrowsAnException_WhenCronExpressionIsInvalid()
        {
            var manager = CreateManager();

            var exception = Assert.Throws<ArgumentException>(
                () => manager.AddOrUpdate(_id, _job, "* * *"));

            Assert.Equal("cronExpression", exception.ParamName);
        }

        [Fact]
        public void AddOrUpdate_ThrowsAnException_WhenCronExpression_HaveInvalidParts()
        {
            var manager = CreateManager();

            var exception = Assert.Throws<ArgumentException>(
                () => manager.AddOrUpdate(_id, _job, "* * * * 9999"));

            Assert.Equal("cronExpression", exception.ParamName);
        }

        [Fact]
        public void AddOrUpdate_ThrowsAnException_WhenTimeZoneIsNull()
        {
            var manager = CreateManager();

            var exception = Assert.Throws<ArgumentNullException>(
                () => manager.AddOrUpdate(_id, _job, _cronExpression, (TimeZoneInfo) null));

            Assert.Equal("timeZone", exception.ParamName);
        }

        [Fact]
        public void AddOrUpdate_ThrowsAnException_WhenOptionsArgumentIsNull()
        {
            var manager = CreateManager();

            var exception = Assert.Throws<ArgumentNullException>(
                () => manager.AddOrUpdate(_id, _job, _cronExpression, null));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void AddOrUpdate_ThrowsAnException_WhenQueueIsNull()
        {
            var manager = CreateManager();

            var exception = Assert.Throws<ArgumentNullException>(
                () => manager.AddOrUpdate(_id, _job, _cronExpression, TimeZoneInfo.Utc, null));

            Assert.Equal("queue", exception.ParamName);
        }

        [Fact]
        public void AddOrUpdate_AddsAJob_ToTheRecurringJobsSet()
        {
            var manager = CreateManager();

            manager.AddOrUpdate(_id, _job, _cronExpression);

            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, JobHelper.ToTimestamp(_now)));
        }

        [Fact]
        public void AddOrUpdate_SetsTheRecurringJobEntry()
        {
            var manager = CreateManager();

            manager.AddOrUpdate(_id, _job, _cronExpression);

            _transaction.Verify(x => x.SetRangeInHash(
                $"recurring-job:{_id}",
                It.Is<Dictionary<string, string>>(rj => 
                    rj["Cron"] == "* * * * *"
                    && !String.IsNullOrEmpty(rj["Job"])
                    && JobHelper.DeserializeDateTime(rj["CreatedAt"]) > _now.AddMinutes(-1))));
        }

        [Fact]
        public void AddOrUpdate_CommitsTransaction()
        {
            var manager = CreateManager();

            manager.AddOrUpdate(_id, _job, _cronExpression);

            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void AddOrUpdate_DoesNotUpdateCreatedAtValue_OfExistingJobs()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}"))
                .Returns(new Dictionary<string, string> { { "CreatedAt", JobHelper.SerializeDateTime(_now) } });

            var manager = CreateManager();

            // Act
            manager.AddOrUpdate(_id, _job, _cronExpression);

            // Assert
            _transaction.Verify(
                x => x.SetRangeInHash(
                    $"recurring-job:{_id}",
                    It.Is<Dictionary<string, string>>(rj => rj.ContainsKey("CreatedAt"))),
                Times.Never);
        }

        [Fact]
        public void AddOrUpdate_IsAbleToScheduleSecondBasedCronExpression()
        {
            var manager = CreateManager();

            manager.AddOrUpdate(_id, _job, "15 * * * * *");

            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, JobHelper.ToTimestamp(_now.AddSeconds(15))));
        }

        [Fact]
        public void AddOrUpdate_EnsuresExistingOldJobsAreUpdated()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}")).Returns(new Dictionary<string, string>
            {
                { "Cron", _cronExpression },
                { "Job", InvocationData.Serialize(_job).SerializePayload() },
                { "CreatedAt", JobHelper.SerializeDateTime(_now) },
                { "NextExecution", JobHelper.SerializeDateTime(_now) },
                { "Queue", "default" },
                { "TimeZoneId", "UTC" },
                { "LastJobId", "1384" }
            });

            var manager = CreateManager();

            // Act
            manager.AddOrUpdate(_id, _job, _cronExpression);

            // Assert
            _transaction.Verify(x => x.SetRangeInHash(
                $"recurring-job:{_id}", 
                It.Is<Dictionary<string, string>>(dict => dict.Count == 1 && dict["V"] == "2")));

            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, JobHelper.ToTimestamp(_now)));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void AddOrUpdate_CanAddRecurringJob_WithCronThatNeverFires()
        {
            // Arrange
            var manager = CreateManager();

            // Act
            manager.AddOrUpdate(_id, _job, "0 0 31 2 *");

            // Assert
            _transaction.Verify(x => x.SetRangeInHash(
                $"recurring-job:{_id}", 
                It.Is<Dictionary<string, string>>(dict => 
                    dict.ContainsKey("Cron") && dict["Cron"] == "0 0 31 2 *" &&
                    !dict.ContainsKey("NextExecution"))));

            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, -1.0D));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void AddOrUpdate_UsesTimeZoneResolver_WhenCalculatingNextExecution()
        {
            // Arrange
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(PlatformHelper.IsRunningOnWindows()
                ? "Hawaiian Standard Time"
                : "Pacific/Honolulu");

            _timeZoneResolver.Setup(x => x.GetTimeZoneById(It.IsAny<string>())).Throws<InvalidOperationException>();
            _timeZoneResolver
                .Setup(x => x.GetTimeZoneById(It.Is<string>(id => id == "Hawaiian Standard Time" || id == "Pacific/Honolulu")))
                .Returns(timeZone);

            // We are returning IANA time zone on Windows and Windows time zone on Linux.
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}")).Returns(new Dictionary<string, string>
            {
                { "Cron", "0 0 * * *" },
                { "Job", InvocationData.Serialize(_job).SerializePayload() },
                { "CreatedAt", JobHelper.SerializeDateTime(_now) },
                { "TimeZoneId", PlatformHelper.IsRunningOnWindows() ? "Pacific/Honolulu" : "Hawaiian Standard Time" },
                { "NextExecution", JobHelper.SerializeDateTime(_now.AddHours(18).AddMinutes(30)) },
                { "Queue", "default" },
                { "V", "2" }
            });

            var manager = CreateManager();

            // Act
            manager.AddOrUpdate(_id, _job, "0 0 * * *", timeZone, "default");

            // Assert
            _transaction.Verify(x => x.SetRangeInHash($"recurring-job:{_id}", It.Is<Dictionary<string, string>>(dict =>
                dict.ContainsKey("TimeZoneId") && !dict.ContainsKey("NextExecution"))));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, JobHelper.ToTimestamp(_now.AddHours(18).AddMinutes(30))));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void AddOrUpdate_DoesNotReScheduleJob_WhenUpdatingIt()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}")).Returns(new Dictionary<string, string>
            {
                { "Cron", "* * * * *" },
                { "Job", InvocationData.Serialize(_job).SerializePayload() },
                { "CreatedAt", JobHelper.SerializeDateTime(_now.AddMinutes(-3)) },
                { "LastExecution", JobHelper.SerializeDateTime(_now.AddMinutes(-2)) },
                { "NextExecution", JobHelper.SerializeDateTime(_now.AddMinutes(-1)) }
            });

            var manager = CreateManager();

            // Act
            manager.AddOrUpdate(_id, _job, "* * * * *");

            // Assert
            _transaction.Verify(x => x.SetRangeInHash($"recurring-job:{_id}", It.Is<Dictionary<string, string>>(dict =>
                !dict.ContainsKey("NextExecution") || dict["NextExecution"] == JobHelper.SerializeDateTime(_now.AddMinutes(-1)))));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, JobHelper.ToTimestamp(_now.AddMinutes(-1))));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void AddOrUpdate_CanUpdateRecurringJobs_WhoseMethodCouldNotBeFound()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}")).Returns(new Dictionary<string, string>
            {
                { "Cron", "* * * * *" },
                { "Job", InvocationData.Serialize(_job).SerializePayload().Replace("Hangfire", "Test") },
                { "CreatedAt", JobHelper.SerializeDateTime(_now.AddMinutes(-2)) },
                { "LastExecution", JobHelper.SerializeDateTime(_now.AddMinutes(-1)) },
                { "NextExecution", JobHelper.SerializeDateTime(_now) }
            });

            var manager = CreateManager();

            // Act
            manager.AddOrUpdate(_id, _job, "* * * * *");

            // Assert
            _transaction.Verify(x => x.SetRangeInHash($"recurring-job:{_id}", It.Is<Dictionary<string, string>>(
                dict => dict.ContainsKey("Job") && dict["Job"].Contains("Hangfire.Core.Tests"))));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, JobHelper.ToTimestamp(_now)));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void AddOrUpdate_CanUpdateRecurringJobs_WhoseJobPropertyCanNotBeDeserialized()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}")).Returns(new Dictionary<string, string>
            {
                { "Cron", "* * * * *" },
                { "Job", "some garbage" },
                { "CreatedAt", JobHelper.SerializeDateTime(_now.AddMinutes(-2)) },
                { "LastExecution", JobHelper.SerializeDateTime(_now.AddMinutes(-1)) },
                { "NextExecution", JobHelper.SerializeDateTime(_now) }
            });

            var manager = CreateManager();

            // Act
            manager.AddOrUpdate(_id, _job, "* * * * *");

            // Assert
            _transaction.Verify(x => x.SetRangeInHash($"recurring-job:{_id}", It.Is<Dictionary<string, string>>(
                dict => dict.ContainsKey("Job") && dict["Job"].Contains("Hangfire.Core.Tests"))));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, JobHelper.ToTimestamp(_now)));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void AddOrUpdate_ResetsRetryAttemptNumber_WhenUpdatingARecurringJob()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}")).Returns(new Dictionary<string, string>
            {
                { "Cron", "* * * * *" },
                { "Job", "some garbage" },
                { "RetryAttempt", "10" }
            });
            
            var manager = CreateManager();

            // Act
            manager.AddOrUpdate(_id, _job, "* * * * *");

            // Assert
            _transaction.Verify(x => x.SetRangeInHash($"recurring-job:{_id}", It.Is<Dictionary<string, string>>(
                dict => dict.ContainsKey("RetryAttempt") && dict["RetryAttempt"] == "0")));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void AddOrUpdate_UsesCurrentTime_InsteadOfLastExecution_ToCalculateNextExecution_WhenChangingCronExpression()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}")).Returns(new Dictionary<string, string>
            {
                { "Cron", "30 12 * * *" },
                { "Job", InvocationData.Serialize(_job).SerializePayload() },
                { "LastExecution", JobHelper.SerializeDateTime(_now.AddHours(-3)) }
            });

            var manager = CreateManager();

            // Act
            manager.AddOrUpdate(_id, _job, "30 13 * * *");

            // Assert
            _transaction.Verify(x => x.SetRangeInHash($"recurring-job:{_id}", It.Is<Dictionary<string, string>>(dict =>
                JobHelper.DeserializeDateTime(dict["NextExecution"]) == _now.AddHours(22))));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, JobHelper.ToTimestamp(_now.AddHours(22))));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void AddOrUpdate_UsesCurrentTime_InsteadOfLastExecution_ToCalculateNextExecution_WhenChangingTimeZone()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}")).Returns(new Dictionary<string, string>
            {
                { "Cron", "30 13 * * *" },
                { "Job", InvocationData.Serialize(_job).SerializePayload() },
                { "TimeZoneId", PlatformHelper.IsRunningOnWindows() ? "Pacific/Honolulu" : "Hawaiian Standard Time" },
                { "LastExecution", JobHelper.SerializeDateTime(_now.AddDays(-3)) }
            });

            var manager = CreateManager();

            // Act
            manager.AddOrUpdate(_id, _job, "30 13 * * *", TimeZoneInfo.Utc);

            // Assert
            _transaction.Verify(x => x.SetRangeInHash($"recurring-job:{_id}", It.Is<Dictionary<string, string>>(dict =>
                JobHelper.DeserializeDateTime(dict["NextExecution"]) == _now.AddHours(22))));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, JobHelper.ToTimestamp(_now.AddHours(22))));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void AddOrUpdate_CanRecoverARecurringJob_FromErrorState_WithoutSchedulingToThePast()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}")).Returns(new Dictionary<string, string>
            {
                { "Cron", "* * * * *" },
                { "Job", InvocationData.Serialize(_job).SerializePayload() },
                { "CreatedAt", JobHelper.SerializeDateTime(_now.AddDays(-1)) },
                { "Error", "Some error that's gone" },
                { "NextExecution", String.Empty },
                { "TimeZoneId", "UTC" },
                { "LastJobId", "1384" }
            });

            var manager = CreateManager();

            // Act
            manager.AddOrUpdate(_id, _job, "* * * * *");

            // Assert
            _transaction.Verify(x => x.SetRangeInHash(
                $"recurring-job:{_id}",
                It.Is<Dictionary<string, string>>(dict =>
                    dict.ContainsKey("Error") && dict["Error"] == String.Empty &&
                    dict.ContainsKey("NextExecution") && JobHelper.DeserializeDateTime(dict["NextExecution"]) == _now)));

            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, JobHelper.ToTimestamp(_now)));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void AddOrUpdate_CanNotTriggerRecurringJob_WhenNextExecutionTimeIsInFuture()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}")).Returns(new Dictionary<string, string>
            {
                { "Cron", "* * * * *" },
                { "Job", InvocationData.Serialize(_job).SerializePayload() },
                { "CreatedAt", JobHelper.SerializeDateTime(_now.AddDays(-1)) },
                { "LastExecution", JobHelper.SerializeDateTime(_now.AddMinutes(-5)) },
                { "NextExecution", JobHelper.SerializeDateTime(_now.AddMinutes(1)) },
                { "TimeZoneId", "UTC" }
            });

            var manager = CreateManager();

            // Act
            manager.AddOrUpdate(_id, _job, "* * * * *");

            // Assert
            _transaction.Verify(x => x.SetRangeInHash(
                $"recurring-job:{_id}",
                It.Is<Dictionary<string, string>>(dict =>
                    !dict.ContainsKey("NextExecution") || dict["NextExecution"] == JobHelper.SerializeDateTime(_now.AddMinutes(1)))));

            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, JobHelper.ToTimestamp(_now.AddMinutes(1))));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void Trigger_ThrowsAnException_WhenIdIsNull()
        {
            var manager = CreateManager();

            Assert.Throws<ArgumentNullException>(() => manager.Trigger(null));
        }

        [Fact]
        public void Trigger_EnqueuesScheduledJob()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}"))
                .Returns(new Dictionary<string, string>
                {
                    { "Job", JobHelper.ToJson(InvocationData.Serialize(Job.FromExpression(() => Console.WriteLine()))) },
                    { "Cron", Cron.Minutely() }
                });

            var manager = CreateManager();

            // Act
            manager.Trigger(_id);

            // Assert
            _stateMachine.Verify(x => x.ApplyState(
                It.Is<ApplyStateContext>(context => context.NewState is EnqueuedState)));
        }

        [Fact]
        public void Trigger_EnqueuedJobToTheSpecificQueue_IfSpecified()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}"))
                .Returns(new Dictionary<string, string>
                {
                    { "Job", JobHelper.ToJson(InvocationData.Serialize(_job)) },
                    { "Cron", _cronExpression },
                    { "Queue", "my_queue" }
                });

            var manager = CreateManager();

            // Act
            manager.Trigger(_id);

            // Assert
            _stateMachine.Verify(x => x.ApplyState(It.Is<ApplyStateContext>(context =>
                ((EnqueuedState)context.NewState).Queue == "my_queue")));
        }

        [Fact]
        public void Trigger_DoesNotThrowIfJobDoesNotExist()
        {
            var manager = CreateManager();

            manager.Trigger(_id);

            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
        }

        [Fact]
        public void Trigger_CanTriggerRecurringJob_WithCronThatNeverFires()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}"))
                .Returns(new Dictionary<string, string>
                {
                    { "Job", JobHelper.ToJson(InvocationData.Serialize(_job)) },
                    { "Cron", "0 0 31 2 *" },
                });

            var manager = CreateManager();

            // Act
            manager.Trigger(_id);

            // Assert
            _stateMachine.Verify(x => x.ApplyState(It.IsAny<ApplyStateContext>()));

            _transaction.Verify(x => x.SetRangeInHash($"recurring-job:{_id}", It.Is<Dictionary<string, string>>(dict =>
                dict.ContainsKey("LastExecution") && dict["LastExecution"] == JobHelper.SerializeDateTime(_now) &&
                !dict.ContainsKey("NextExecution"))));

            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, -1.0D));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void Trigger_SchedulesNextExecution_DependingOnCurrentTime_ToTheFuture()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}")).Returns(new Dictionary<string, string>
            {
                { "Cron", "* * * * *" },
                { "Job", InvocationData.Serialize(_job).SerializePayload() },
                { "CreatedAt", JobHelper.SerializeDateTime(_now.AddMinutes(-3)) },
                { "LastExecution", JobHelper.SerializeDateTime(_now.AddMinutes(-2)) },
                { "NextExecution", JobHelper.SerializeDateTime(_now.AddMinutes(-1)) }
            });

            var manager = CreateManager();

            // Act
            manager.Trigger(_id);

            // Assert
            _transaction.Verify(x => x.SetRangeInHash($"recurring-job:{_id}", It.Is<Dictionary<string, string>>(dict =>
                dict["NextExecution"] == JobHelper.SerializeDateTime(_now.AddMinutes(1)))));
            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id, JobHelper.ToTimestamp(_now.AddMinutes(1))));
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void Trigger_ThrowsAnException_WhenRecurringJobCanNotBeTriggered_AndDoesNotCreateBackgroundJob()
        {
            // Arrange
            _timeZoneResolver.Setup(x => x.GetTimeZoneById(It.IsAny<string>())).Throws<Exception>();
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}"))
                .Returns(new Dictionary<string, string>
                {
                    { "Job", JobHelper.ToJson(InvocationData.Serialize(Job.FromExpression(() => Console.WriteLine()))) },
                    { "Cron", Cron.Minutely() },
                    { "TimeZoneId", "UnexistingID" }
                });

            var manager = CreateManager();

            // Act
            Assert.Throws<AggregateException>(() => manager.Trigger(_id));

            // Assert
            _stateMachine.Verify(x => x.ApplyState(It.IsAny<ApplyStateContext>()), Times.Never);
        }

        [Fact]
        public void RemoveIfExists_ThrowsAnException_WhenIdIsNull()
        {
            var manager = CreateManager();

            Assert.Throws<ArgumentNullException>(
                () => manager.RemoveIfExists(null));
        }

        [Fact]
        public void RemoveIfExists_RemovesEntriesAndCommitsTheTransaction()
        {
            var manager = CreateManager();

            manager.RemoveIfExists(_id);

            _transaction.Verify(x => x.RemoveFromSet("recurring-jobs", _id));
            _transaction.Verify(x => x.RemoveHash($"recurring-job:{_id}"));
            _transaction.Verify(x => x.Commit());
        }

        [Fact, CleanSerializerSettings]
        public void HandlesChangingProcessOfInvocationDataSerialization()
        {
            SerializationHelper.SetUserSerializerSettings(SerializerSettingsHelper.DangerousSettings);

            var initialJob = Job.FromExpression(() => Console.WriteLine());
            var invocationData = InvocationData.Serialize(initialJob);

            var serializedInvocationData = SerializationHelper.Serialize(invocationData, SerializationOption.User);

            var deserializedInvocationData = SerializationHelper.Deserialize<InvocationData>(serializedInvocationData);
            var deserializedJob = deserializedInvocationData.Deserialize();

            Assert.Equal(initialJob.Args, deserializedJob.Args);
            Assert.Equal(initialJob.Method, deserializedJob.Method);
            Assert.Equal(initialJob.Type, deserializedJob.Type);
        }

        private RecurringJobManager CreateManager()
        {
            return new RecurringJobManager(_storage.Object, _factory.Object, _timeZoneResolver.Object, _nowFactory);
        }

        public static void Method() { }
    }
}
