using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Newtonsoft.Json;
using Xunit;

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

        public RecurringJobManagerFacts()
        {
            _id = "recurring-job-id";
            _job = Job.FromExpression(() => Method());
            _cronExpression = Cron.Minutely();
            _storage = new Mock<JobStorage>();
            _factory = new Mock<IBackgroundJobFactory>();

            _connection = new Mock<IStorageConnection>();
            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);
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
                () => new RecurringJobManager(_storage.Object, null));

            Assert.Equal("factory", exception.ParamName);
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

            _transaction.Verify(x => x.AddToSet("recurring-jobs", _id));
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
                    && JobHelper.DeserializeDateTime(rj["CreatedAt"]) > DateTime.UtcNow.AddMinutes(-1))));
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
                .Returns(new Dictionary<string, string>());

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
                    { "Job", JobHelper.ToJson(InvocationData.Serialize(Job.FromExpression(() => Console.WriteLine()))) }
                });

            var manager = CreateManager();

            // Act
            manager.Trigger(_id);

            // Assert
            _factory.Verify(x => x.Create(It.Is<CreateContext>(context => context.InitialState is EnqueuedState)));
        }

        [Fact]
        public void Trigger_EnqueuedJobToTheSpecificQueue_IfSpecified()
        {
            // Arrange
            _connection.Setup(x => x.GetAllEntriesFromHash($"recurring-job:{_id}"))
                .Returns(new Dictionary<string, string>
                {
                    { "Job", JobHelper.ToJson(InvocationData.Serialize(Job.FromExpression(() => Console.WriteLine()))) },
                    { "Queue", "my_queue" }
                });

            var manager = CreateManager();

            // Act
            manager.Trigger(_id);

            // Assert
            _factory.Verify(x => x.Create(It.Is<CreateContext>(context =>
                ((EnqueuedState)context.InitialState).Queue == "my_queue")));
        }

        [Fact]
        public void Trigger_DoesNotThrowIfJobDoesNotExist()
        {
            var manager = CreateManager();

            manager.Trigger(_id);

            _factory.Verify(x => x.Create(It.IsAny<CreateContext>()), Times.Never);
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

        [Fact, CleanJsonSerializersSettings]
        public void HandlesChangingCoreSerializerSettings()
        {
            var previousSerializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,

                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,

                Formatting = Formatting.Indented,

                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
            };
            JobHelper.SetSerializerSettings(previousSerializerSettings);

            var initialJob = Job.FromExpression(() => Console.WriteLine());
            var invocationData = InvocationData.Serialize(initialJob);

            var serializedInvocationData = JobHelper.ToJson(invocationData);

            var deserializedInvocationData = JobHelper.Deserialize<InvocationData>(serializedInvocationData);
            var deserializedJob = deserializedInvocationData.Deserialize();

            Assert.Equal(initialJob.Args, deserializedJob.Args);
            Assert.Equal(initialJob.Method, deserializedJob.Method);
            Assert.Equal(initialJob.Type, deserializedJob.Type);
        }

        private RecurringJobManager CreateManager()
        {
            return new RecurringJobManager(_storage.Object, _factory.Object);
        }

        public static void Method() { }
    }
}
