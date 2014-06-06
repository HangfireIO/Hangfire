using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests
{
    public class RecurringJobManagerFacts
    {
        private readonly Mock<JobStorage> _storage;
        private readonly string _id;
        private readonly Job _job;
        private readonly string _cronExpression;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IWriteOnlyTransaction> _transaction;

        public RecurringJobManagerFacts()
        {
            _id = "recurring-job-id";
            _job = Job.FromExpression(() => Method());
            _cronExpression = Cron.Minutely();
            _storage = new Mock<JobStorage>();
            
            _connection = new Mock<IStorageConnection>();
            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
            
            _transaction = new Mock<IWriteOnlyTransaction>();
            _connection.Setup(x => x.CreateWriteTransaction()).Returns(_transaction.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RecurringJobManager(null));

            Assert.Equal("storage", exception.ParamName);
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
            return new RecurringJobManager(_storage.Object);
        }

        public static void Method() { }
    }
}
