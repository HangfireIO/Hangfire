﻿using System;
using System.Collections.Generic;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

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
        private readonly Mock<IBackgroundJobClient> _client;

        public RecurringJobManagerFacts()
        {
            _id = "recurring-job-id";
            _job = Job.FromExpression(() => Method());
            _cronExpression = Cron.Minutely();
            _storage = new Mock<JobStorage>();
            _client = new Mock<IBackgroundJobClient>();
            
            _connection = new Mock<IStorageConnection>();
            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
            
            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RecurringJobManager(null, _client.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RecurringJobManager(_storage.Object, null));

            Assert.Equal("client", exception.ParamName);
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
                String.Format("recurring-job:{0}", _id),
                It.Is<Dictionary<string, string>>(rj => 
                    rj["Cron"] == "* * * * *" && !String.IsNullOrEmpty(rj["Job"]))));
        }

        [Fact]
        public void AddOrUpdate_CommitsTransaction()
        {
            var manager = CreateManager();

            manager.AddOrUpdate(_id, _job, _cronExpression);

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
            _connection.Setup(x => x.GetAllEntriesFromHash(String.Format("recurring-job:{0}", _id)))
                .Returns(new Dictionary<string, string>
                {
                    { "Job", JobHelper.ToJson(InvocationData.Serialize(Job.FromExpression(() => Console.WriteLine()))) }
                });

            var manager = CreateManager();

            // Act
            manager.Trigger(_id);

            // Assert
            _client.Verify(x => x.Create(It.IsNotNull<Job>(), It.IsAny<EnqueuedState>()));
        }

        [Fact]
        public void Trigger_DoesNotThrowIfJobDoesNotExist()
        {
            var manager = CreateManager();

            Assert.DoesNotThrow(() => manager.Trigger(_id));
            _client.Verify(
                x => x.Create(It.IsAny<Job>(), It.IsAny<EnqueuedState>()),
                Times.Never);
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
            _transaction.Verify(x => x.RemoveHash(String.Format("recurring-job:{0}", _id)));
            _transaction.Verify(x => x.Commit());
        }

        private RecurringJobManager CreateManager()
        {
            return new RecurringJobManager(_storage.Object, _client.Object);
        }

        public static void Method() { }
    }
}
