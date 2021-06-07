extern alias ReferencedDapper;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using ReferencedDapper::Dapper;
using Hangfire.States;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerWriteOnlyTransactionFacts
    {
        private static readonly string TooLongKey = "123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_12345";
        private static readonly string TooLongTruncatedKey = TooLongKey.Substring(0, 100);

        private readonly PersistentJobQueueProviderCollection _queueProviders;

        public SqlServerWriteOnlyTransactionFacts()
        {
            var defaultProvider = new Mock<IPersistentJobQueueProvider>();
            defaultProvider.Setup(x => x.GetJobQueue())
                .Returns(new Mock<IPersistentJobQueue>().Object);

            _queueProviders = new PersistentJobQueueProviderCollection(defaultProvider.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_IfStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerWriteOnlyTransaction(null, () => null));

            Assert.Equal("storage", exception.ParamName);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void ExpireJob_ThrowsAnException_WhenJobIdIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.ExpireJob(null, TimeSpan.Zero), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("jobId", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void ExpireJob_SetsJobExpirationData(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnection(sql =>
            {
                var jobId = sql.Query(arrangeSql).Single().Id.ToString();
                var anotherJobId = sql.Query(arrangeSql).Single().Id.ToString();

                Commit(x => x.ExpireJob(jobId, TimeSpan.FromHours(24)), useMicrosoftDataSqlClient, useBatching);

                var job = GetTestJob(sql, jobId);
                Assert.True(DateTime.UtcNow.AddHours(23) < job.ExpireAt && job.ExpireAt < DateTime.UtcNow.AddHours(25));

                var anotherJob = GetTestJob(sql, anotherJobId);
                Assert.Null(anotherJob.ExpireAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void PersistJob_ThrowsAnException_WhenJobIdIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.PersistJob(null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("jobId", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void PersistJob_ClearsTheJobExpirationData(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt, ExpireAt)
values ('', '', getutcdate(), getutcdate())
select scope_identity() as Id";

            UseConnection(sql =>
            {
                var jobId = sql.Query(arrangeSql).Single().Id.ToString();
                var anotherJobId = sql.Query(arrangeSql).Single().Id.ToString();

                Commit(x => x.PersistJob(jobId), useMicrosoftDataSqlClient, useBatching);

                var job = GetTestJob(sql, jobId);
                Assert.Null(job.ExpireAt);

                var anotherJob = GetTestJob(sql, anotherJobId);
                Assert.NotNull(anotherJob.ExpireAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetJobState_ThrowsAnException_WhenJobIdIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.SetJobState(null, new Mock<IState>().Object), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("jobId", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetJobState_ThrowsAnException_WhenStateIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.SetJobState("my-job", null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("state", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetJobState_AppendsAStateAndSetItToTheJob(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnection(sql =>
            {
                var jobId = sql.Query(arrangeSql).Single().Id.ToString();
                var anotherJobId = sql.Query(arrangeSql).Single().Id.ToString();

                var state = new Mock<IState>();
                state.Setup(x => x.Name).Returns("State");
                state.Setup(x => x.Reason).Returns("Reason");
                state.Setup(x => x.SerializeData())
                    .Returns(new Dictionary<string, string> { { "Name", "Value" } });

                Commit(x => x.SetJobState(jobId, state.Object), useMicrosoftDataSqlClient, useBatching);

                var job = GetTestJob(sql, jobId);
                Assert.Equal("State", job.StateName);
                Assert.NotNull(job.StateId);

                var anotherJob = GetTestJob(sql, anotherJobId);
                Assert.Null(anotherJob.StateName);
                Assert.Null(anotherJob.StateId);

                var jobState = sql.Query($"select * from [{Constants.DefaultSchema}].State").Single();
                Assert.Equal((string)jobId, jobState.JobId.ToString());
                Assert.Equal("State", jobState.Name);
                Assert.Equal("Reason", jobState.Reason);
                Assert.NotNull(jobState.CreatedAt);
                Assert.Equal("{\"Name\":\"Value\"}", jobState.Data);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetJobState_CanBeCalledWithNullReasonAndData(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnection(sql =>
            {
                var jobId = sql.Query(arrangeSql).Single().Id.ToString();

                var state = new Mock<IState>();
                state.Setup(x => x.Name).Returns("State");
                state.Setup(x => x.Reason).Returns((string)null);
                state.Setup(x => x.SerializeData()).Returns((Dictionary<string, string>)null);

                Commit(x => x.SetJobState(jobId, state.Object), useMicrosoftDataSqlClient, useBatching);

                var job = GetTestJob(sql, jobId);
                Assert.Equal("State", job.StateName);
                Assert.NotNull(job.StateId);

                var jobState = sql.Query($"select * from [{Constants.DefaultSchema}].State").Single();
                Assert.Equal("State", jobState.Name);
                Assert.Equal(null, jobState.Reason);
                Assert.Equal(null, jobState.Data);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddJobState_ThrowsAnException_WhenJobIdIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.AddJobState(null, new Mock<IState>().Object), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("jobId", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddJobState_ThrowsAnException_WhenStateIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.AddJobState("my-job", null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("state", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddJobState_JustAddsANewRecordInATable(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnection(sql =>
            {
                var jobId = sql.Query(arrangeSql).Single().Id.ToString();

                var state = new Mock<IState>();
                state.Setup(x => x.Name).Returns("State");
                state.Setup(x => x.Reason).Returns("Reason");
                state.Setup(x => x.SerializeData())
                    .Returns(new Dictionary<string, string> { { "Name", "Value" } });

                Commit(x => x.AddJobState(jobId, state.Object), useMicrosoftDataSqlClient, useBatching);

                var job = GetTestJob(sql, jobId);
                Assert.Null(job.StateName);
                Assert.Null(job.StateId);

                var jobState = sql.Query($"select * from [{Constants.DefaultSchema}].State").Single();
                Assert.Equal((string)jobId, jobState.JobId.ToString());
                Assert.Equal("State", jobState.Name);
                Assert.Equal("Reason", jobState.Reason);
                Assert.NotNull(jobState.CreatedAt);
                Assert.Equal("{\"Name\":\"Value\"}", jobState.Data);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddJobState_CanBeCalledWithNullReasonAndData(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Job (InvocationData, Arguments, CreatedAt)
values ('', '', getutcdate())
select scope_identity() as Id";

            UseConnection(sql =>
            {
                var jobId = sql.Query(arrangeSql).Single().Id.ToString();

                var state = new Mock<IState>();
                state.Setup(x => x.Name).Returns("State");
                state.Setup(x => x.Reason).Returns((string)null);
                state.Setup(x => x.SerializeData()).Returns((Dictionary<string, string>)null);

                Commit(x => x.AddJobState(jobId, state.Object), useMicrosoftDataSqlClient, useBatching);

                var job = GetTestJob(sql, jobId);
                Assert.Null(job.StateName);
                Assert.Null(job.StateId);

                var jobState = sql.Query($"select * from [{Constants.DefaultSchema}].State").Single();
                Assert.Equal((string)jobId, jobState.JobId.ToString());
                Assert.Equal("State", jobState.Name);
                Assert.Equal(null, jobState.Reason);
                Assert.NotNull(jobState.CreatedAt);
                Assert.Equal(null, jobState.Data);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToQueue_ThrowsAnException_WhenQueueIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.AddToQueue(null, "my-job"), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("queue", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToQueue_ThrowsAnException_WhenJobIdIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.AddToQueue("my-queue", null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("jobId", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToQueue_CallsEnqueue_OnTargetPersistentQueue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var correctJobQueue = new Mock<IPersistentJobQueue>();
            var correctProvider = new Mock<IPersistentJobQueueProvider>();
            correctProvider.Setup(x => x.GetJobQueue())
                .Returns(correctJobQueue.Object);

            _queueProviders.Add(correctProvider.Object, new[] { "default" });

            UseConnection(sql =>
            {
                Commit(x => x.AddToQueue("default", "1"), useMicrosoftDataSqlClient, useBatching);

                correctJobQueue.Verify(x => x.Enqueue(
#if NETCOREAPP
                    It.IsNotNull<System.Data.Common.DbConnection>(),
                    It.IsNotNull<System.Data.Common.DbTransaction>(),
#else
                    It.IsNotNull<IDbConnection>(),
#endif
                    "default", 
                    "1"));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToQueue_EnqueuesAJobDirectly_WhenDefaultQueueProviderIsUsed(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            // We are relying on the fact that SqlServerJobQueue.Enqueue method will throw with a negative
            // timeout. If we don't see this exception, and if the record is inserted, then everything is fine.
            var options = new SqlServerStorageOptions { PrepareSchemaIfNecessary = false, CommandTimeout = TimeSpan.FromSeconds(-5) };
            _queueProviders.Add(
                new SqlServerJobQueueProvider(new Mock<SqlServerStorage>("connection=false;", options).Object, options),
                new [] { "default" });

            UseConnection(sql =>
            {
                Commit(x => x.AddToQueue("default", "1"), useMicrosoftDataSqlClient, useBatching);

                var record = sql.Query($"select * from [{Constants.DefaultSchema}].JobQueue").Single();
                Assert.Equal("1", record.JobId.ToString());
                Assert.Equal("default", record.Queue);
                Assert.Null(record.FetchedAt);
            }, useMicrosoftDataSqlClient);
        }

        private static dynamic GetTestJob(IDbConnection connection, string jobId)
        {
            return connection
                .Query($"select * from [{Constants.DefaultSchema}].Job where Id = @id", new { id = jobId })
                .Single();
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void IncrementCounter_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.IncrementCounter(null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void IncrementCounter_ThrowsAnException_WhenKeyIsTooLong(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.ThrowsAny<DbException>(() => 
                    Commit(x => x.IncrementCounter(TooLongKey), useMicrosoftDataSqlClient, useBatching));

                Assert.Contains("data would be truncated", exception.Message);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void IncrementCounter_AddsRecordToCounterTable_WithPositiveValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x => x.IncrementCounter("my-key"), useMicrosoftDataSqlClient, useBatching);

                var record = sql.Query($"select * from [{Constants.DefaultSchema}].Counter").Single();
                
                Assert.Equal("my-key", record.Key);
                Assert.Equal(1, record.Value);
                Assert.Equal((DateTime?)null, record.ExpireAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void IncrementCounter_WithExpiry_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.IncrementCounter(null, TimeSpan.FromHours(1)), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void IncrementCounter_WithExpiry_ThrowsAnException_WhenKeyIsTooLong(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.ThrowsAny<DbException>(() =>
                    Commit(x => x.IncrementCounter(TooLongKey, TimeSpan.FromHours(1)), useMicrosoftDataSqlClient, useBatching));

                Assert.Contains("data would be truncated", exception.Message);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void IncrementCounter_WithExpiry_AddsARecord_WithExpirationTimeSet(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x => x.IncrementCounter("my-key", TimeSpan.FromDays(1)), useMicrosoftDataSqlClient, useBatching);

                var record = sql.Query($"select * from [{Constants.DefaultSchema}].Counter").Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(1, record.Value);
                Assert.NotNull(record.ExpireAt);

                var expireAt = (DateTime) record.ExpireAt;

                Assert.True(DateTime.UtcNow.AddHours(23) < expireAt);
                Assert.True(expireAt < DateTime.UtcNow.AddHours(25));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void IncrementCounter_WithExistingKey_AddsAnotherRecord(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.IncrementCounter("my-key");
                    x.IncrementCounter("my-key");
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].Counter").Single();
                
                Assert.Equal(2, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void DecrementCounter_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.DecrementCounter(null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void DecrementCounter_ThrowsAnException_WhenKeyIsTooLong(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.ThrowsAny<DbException>(() =>
                    Commit(x => x.DecrementCounter(TooLongKey), useMicrosoftDataSqlClient, useBatching));

                Assert.Contains("data would be truncated", exception.Message);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void DecrementCounter_AddsRecordToCounterTable_WithNegativeValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x => x.DecrementCounter("my-key"), useMicrosoftDataSqlClient, useBatching);

                var record = sql.Query($"select * from [{Constants.DefaultSchema}].Counter").Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(-1, record.Value);
                Assert.Equal((DateTime?)null, record.ExpireAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void DecrementCounter_WithExpiry_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.DecrementCounter(null, TimeSpan.FromHours(1)), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void DecrementCounter_WithExpiry_ThrowsAnException_WhenKeyIsTooLong(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.ThrowsAny<DbException>(() =>
                    Commit(x => x.DecrementCounter(TooLongKey, TimeSpan.FromHours(1)), useMicrosoftDataSqlClient, useBatching));

                Assert.Contains("data would be truncated", exception.Message);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void DecrementCounter_WithExpiry_AddsARecord_WithExpirationTimeSet(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x => x.DecrementCounter("my-key", TimeSpan.FromDays(1)), useMicrosoftDataSqlClient, useBatching);

                var record = sql.Query($"select * from [{Constants.DefaultSchema}].Counter").Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(-1, record.Value);
                Assert.NotNull(record.ExpireAt);

                var expireAt = (DateTime)record.ExpireAt;

                Assert.True(DateTime.UtcNow.AddHours(23) < expireAt);
                Assert.True(expireAt < DateTime.UtcNow.AddHours(25));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void DecrementCounter_WithExistingKey_AddsAnotherRecord(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.DecrementCounter("my-key");
                    x.DecrementCounter("my-key");
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].Counter").Single();

                Assert.Equal(2, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.AddToSet(null, "value"), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_ThrowsAnException_WhenValueIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.AddToSet("my-set", null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("value", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_ThrowsAnException_WhenKeyIsTooLong(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.ThrowsAny<DbException>(() =>
                    Commit(x => x.AddToSet(TooLongKey, "value"), useMicrosoftDataSqlClient, useBatching));

                Assert.Contains("data would be truncated", exception.Message);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_AddsARecord_IfThereIsNo_SuchKeyAndValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x => x.AddToSet("my-key", "my-value"), useMicrosoftDataSqlClient, useBatching);

                var record = sql.Query($"select * from [{Constants.DefaultSchema}].[Set]").Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
                Assert.Equal(0.0, record.Score, 2);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_AddsARecord_WhenKeyIsExists_ButValuesAreDifferent(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "another-value");
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].[Set]").Single();

                Assert.Equal(2, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_DoesNotAddARecord_WhenBothKeyAndValueAreExist(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "my-value");
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].[Set]").Single();
                
                Assert.Equal(1, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_WithScore_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.AddToSet(null, "value", 1.2D), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_WithScore_ThrowsAnException_WhenValueIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.AddToSet("my-set", null, 1.2D), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("value", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_WithScore_ThrowsAnException_WhenKeyIsTooLong(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.ThrowsAny<DbException>(() =>
                    Commit(x => x.AddToSet(TooLongKey, "value", 1.2D), useMicrosoftDataSqlClient, useBatching));

                Assert.Contains("data would be truncated", exception.Message);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_WithScore_AddsARecordWithScore_WhenBothKeyAndValueAreNotExist(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x => x.AddToSet("my-key", "my-value", 3.2), useMicrosoftDataSqlClient, useBatching);

                var record = sql.Query($"select * from [{Constants.DefaultSchema}].[Set]").Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
                Assert.Equal(3.2, record.Score, 3);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_WithScore_UpdatesAScore_WhenBothKeyAndValueAreExist(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "my-value", 3.2);
                }, useMicrosoftDataSqlClient, useBatching);

                var record = sql.Query($"select * from [{Constants.DefaultSchema}].[Set]").Single();

                Assert.Equal(3.2, record.Score, 3);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_WithIgnoreDupKeyOption_InsertsNonExistingValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            try
            {
                UseConnection(sql =>
                {
                    sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Set] REBUILD WITH (IGNORE_DUP_KEY = ON)");

                    Commit(x =>
                        x.AddToSet("my-key","my-value", 3.2),
                        useMicrosoftDataSqlClient,
                        useBatching,
                        options => options.UseIgnoreDupKeyOption = true);

                    var record = sql.Query($"select * from [{Constants.DefaultSchema}].[Set]").Single();
                    Assert.Equal(3.2, record.Score, 3);
                }, useMicrosoftDataSqlClient);
            }
            finally
            {
                UseConnection(sql => sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Set] REBUILD WITH (IGNORE_DUP_KEY = OFF)"), useMicrosoftDataSqlClient);
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_WithIgnoreDupKeyOption_UpdatesExistingValue_WhenIgnoreDupKeyOptionIsSet(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            try
            {
                UseConnection(sql =>
                {
                    sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Set] REBUILD WITH (IGNORE_DUP_KEY = ON)");
                    sql.Execute($@"insert into [{Constants.DefaultSchema}].[Set] ([Key], Value, Score) VALUES
(N'my-key1', N'value1', 1.2),
(N'my-key1', N'value2', 1.2),
(N'my-key2', N'value1', 1.2)");

                    Commit(x =>
                        x.AddToSet("my-key1", "value1", 2.3), 
                        useMicrosoftDataSqlClient, useBatching, options => options.UseIgnoreDupKeyOption = true);

                    var record1 = sql.Query($"select * from [{Constants.DefaultSchema}].[Set] where [Key] = N'my-key1' and Value = N'value1'").Single();
                    Assert.Equal(2.3, record1.Score, 3);

                    var record2 = sql.Query($"select * from [{Constants.DefaultSchema}].[Set] where [Key] = N'my-key1' and Value = N'value2'").Single();
                    Assert.Equal(1.2, record2.Score, 3);

                    var record3 = sql.Query($"select * from [{Constants.DefaultSchema}].[Set] where [Key] = N'my-key2' and Value = N'value1'").Single();
                    Assert.Equal(1.2, record3.Score, 3);
                }, useMicrosoftDataSqlClient);
            }
            finally
            {
                UseConnection(sql => sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Set] REBUILD WITH (IGNORE_DUP_KEY = OFF)"), useMicrosoftDataSqlClient);
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddToSet_WithIgnoreDupKeyOption_FailsToUpdateExistingValue_WhenIgnoreDupKeyOptionWasNotSet(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Set] REBUILD WITH (IGNORE_DUP_KEY = OFF)");
                sql.Execute($"insert into [{Constants.DefaultSchema}].[Set] ([Key], Value, Score) VALUES (N'key1', N'value1', 1.2)");

                var exception = Assert.ThrowsAny<DbException>(() =>
                    Commit(x =>
                        x.AddToSet("key1", "value1"),
                        useMicrosoftDataSqlClient, useBatching, options => options.UseIgnoreDupKeyOption = true));

                Assert.Contains("Violation of PRIMARY KEY", exception.Message);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveFromSet_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.RemoveFromSet(null, "value"), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveFromSet_ThrowsAnException_WhenValueIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.RemoveFromSet("my-set", null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("value", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveFromSet_DoesNotTruncateKey_BeforeUsingIt(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                // Arrange
                Commit(x => x.AddToSet(TooLongTruncatedKey, "value"), useMicrosoftDataSqlClient, useBatching);

                // Act
                Commit(x => x.RemoveFromSet(TooLongKey, "value"), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var result = sql.Query(
                    $"select [Value] from [{Constants.DefaultSchema}].[Set] where [Key] = @key",
                    new { key = TooLongTruncatedKey }).Single();

                Assert.Equal("value", result.Value);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveFromSet_RemovesARecord_WithGivenKeyAndValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("my-key", "my-value");
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].[Set]").Single();

                Assert.Equal(0, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveFromSet_DoesNotRemoveRecord_WithSameKey_AndDifferentValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("my-key", "different-value");
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].[Set]").Single();

                Assert.Equal(1, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveFromSet_DoesNotRemoveRecord_WithSameValue_AndDifferentKey(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("different-key", "my-value");
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].[Set]").Single();

                Assert.Equal(1, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void InsertToList_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.InsertToList(null, "value"), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void InsertToList_ThrowsAnException_WhenValueIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.InsertToList("my-list", null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("value", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void InsertToList_ThrowsAnException_WhenKeyIsTooLong(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.ThrowsAny<DbException>(() =>
                    Commit(x => x.InsertToList(TooLongKey, "value"), useMicrosoftDataSqlClient, useBatching));

                Assert.Contains("data would be truncated", exception.Message);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void InsertToList_AddsARecord_WithGivenValues(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x => x.InsertToList("my-key", "my-value"), useMicrosoftDataSqlClient, useBatching);

                var record = sql.Query($"select * from [{Constants.DefaultSchema}].List").Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void InsertToList_AddsAnotherRecord_WhenBothKeyAndValueAreExist(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.InsertToList("my-key", "my-value");
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].List").Single();

                Assert.Equal(2, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveFromList_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.RemoveFromList(null, "value"), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveFromList_ThrowsAnException_WhenValueIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.RemoveFromList("my-list", null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("value", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveFromList_DoesNotTruncateKey_BeforeUsingIt(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                // Arrange
                Commit(x => x.InsertToList(TooLongTruncatedKey, "value"), useMicrosoftDataSqlClient, useBatching);

                // Act
                Commit(x => x.RemoveFromList(TooLongKey, "value"), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var result = sql.Query(
                    $"select [Value] from [{Constants.DefaultSchema}].[List] where [Key] = @key",
                    new { key = TooLongTruncatedKey }).Single();

                Assert.Equal("value", result.Value);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveFromList_RemovesAllRecords_WithGivenKeyAndValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("my-key", "my-value");
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].List").Single();

                Assert.Equal(0, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveFromList_DoesNotRemoveRecords_WithSameKey_ButDifferentValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("my-key", "different-value");
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].List").Single();

                Assert.Equal(1, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveFromList_DoesNotRemoveRecords_WithSameValue_ButDifferentKey(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("different-key", "my-value");
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].List").Single();

                Assert.Equal(1, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void TrimList_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.TrimList(null, 0, 1), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void TrimList_DoesNotTruncateKey_BeforeUsingIt(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                // Arrange
                Commit(x => x.InsertToList(TooLongTruncatedKey, "value"), useMicrosoftDataSqlClient, useBatching);

                // Act
                Commit(x => x.TrimList(TooLongKey, 1, 2), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var result = sql.Query(
                    $"select [Value] from [{Constants.DefaultSchema}].[List] where [Key] = @key",
                    new { key = TooLongTruncatedKey }).Single();

                Assert.Equal("value", result.Value);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void TrimList_TrimsAList_ToASpecifiedRange(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.InsertToList("my-key", "0");
                    x.InsertToList("my-key", "1");
                    x.InsertToList("my-key", "2");
                    x.InsertToList("my-key", "3");
                    x.TrimList("my-key", 1, 2);
                }, useMicrosoftDataSqlClient, useBatching);

                var records = sql.Query($"select * from [{Constants.DefaultSchema}].List").ToArray();

                Assert.Equal(2, records.Length);
                Assert.Equal("1", records[0].Value);
                Assert.Equal("2", records[1].Value);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void TrimList_RemovesRecordsToEnd_IfKeepAndingAt_GreaterThanMaxElementIndex(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.InsertToList("my-key", "0");
                    x.InsertToList("my-key", "1");
                    x.InsertToList("my-key", "2");
                    x.TrimList("my-key", 1, 100);
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].List").Single();

                Assert.Equal(2, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void TrimList_RemovesAllRecords_WhenStartingFromValue_GreaterThanMaxElementIndex(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("my-key", 1, 100);
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].List").Single();

                Assert.Equal(0, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void TrimList_RemovesAllRecords_IfStartFromGreaterThanEndingAt(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("my-key", 1, 0);
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].List").Single();

                Assert.Equal(0, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void TrimList_RemovesRecords_OnlyOfAGivenKey(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("another-key", 1, 0);
                }, useMicrosoftDataSqlClient, useBatching);

                var recordCount = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].List").Single();

                Assert.Equal(1, recordCount);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetRangeInHash_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.SetRangeInHash(null, new Dictionary<string, string>()), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetRangeInHash_ThrowsAnException_WhenKeyValuePairsArgumentIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.SetRangeInHash("some-hash", null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("keyValuePairs", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetRangeInHash_ThrowsAnException_WhenKeyIsTooLong(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.ThrowsAny<DbException>(() =>
                    Commit(x => x.SetRangeInHash(
                        TooLongKey,
                        new Dictionary<string, string> { { "field", "value" } }), useMicrosoftDataSqlClient, useBatching));

                Assert.Contains("data would be truncated", exception.Message);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetRangeInHash_MergesAllRecords(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    { "Key1", "Value1" },
                    { "Key2", "Value2" }
                }), useMicrosoftDataSqlClient, useBatching);

                var result = sql.Query(
                    $"select * from [{Constants.DefaultSchema}].Hash where [Key] = @key",
                    new { key = "some-hash" })
                    .ToDictionary(x => (string)x.Field, x => (string)x.Value);

                Assert.Equal("Value1", result["Key1"]);
                Assert.Equal("Value2", result["Key2"]);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetRangeInHash_CanSetANullValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Commit(x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    { "Key1", null }
                }), useMicrosoftDataSqlClient, useBatching);

                var result = sql.Query(
                        $"select * from [{Constants.DefaultSchema}].Hash where [Key] = @key",
                        new { key = "some-hash" })
                    .ToDictionary(x => (string)x.Field, x => (string)x.Value);

                Assert.Equal(null, result["Key1"]);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetRangeInHash_WithIgnoreDupKeyOption_InsertsNonExistingValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            try
            {
                UseConnection(sql =>
                {
                    sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Hash] REBUILD WITH (IGNORE_DUP_KEY = ON)");

                    Commit(x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                    {
                        { "key", "value" }
                    }), useMicrosoftDataSqlClient, useBatching, options => options.UseIgnoreDupKeyOption = true);

                    var result = sql
                        .Query($"select * from [{Constants.DefaultSchema}].Hash where [Key] = N'some-hash'")
                        .ToDictionary(x => (string)x.Field, x => (string)x.Value);

                    Assert.Equal("value", result["key"]);
                }, useMicrosoftDataSqlClient);
            }
            finally
            {
                UseConnection(sql => sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Hash] REBUILD WITH (IGNORE_DUP_KEY = OFF)"), useMicrosoftDataSqlClient);
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetRangeInHash_WithIgnoreDupKeyOption_UpdatesExistingValue_WhenIgnoreDupKeyOptionIsSet(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            try
            {
                UseConnection(sql =>
                {
                    sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Hash] REBUILD WITH (IGNORE_DUP_KEY = ON)");
                    sql.Execute($@"insert into [{Constants.DefaultSchema}].Hash([Key], Field, Value) VALUES
(N'some-hash', N'key1', N'value1'),
(N'some-hash', N'key2', N'value1'),
(N'othr-hash', N'key1', N'value1')");

                    Commit(x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                    {
                        { "key1", "value2" }
                    }), useMicrosoftDataSqlClient, useBatching, options => options.UseIgnoreDupKeyOption = true);

                    var someResult = sql
                        .Query($"select * from [{Constants.DefaultSchema}].Hash where [Key] = N'some-hash'")
                        .ToDictionary(x => (string)x.Field, x => (string)x.Value);

                    Assert.Equal("value2", someResult["key1"]);
                    Assert.Equal("value1", someResult["key2"]);

                    var othrResult = sql
                        .Query($"select * from [{Constants.DefaultSchema}].Hash where [Key] = N'othr-hash'")
                        .ToDictionary(x => (string)x.Field, x => (string)x.Value);

                    Assert.Equal("value1", othrResult["key1"]);
                }, useMicrosoftDataSqlClient);
            }
            finally
            {
                UseConnection(sql => sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Hash] REBUILD WITH (IGNORE_DUP_KEY = OFF)"), useMicrosoftDataSqlClient);
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetRangeInHash_WithIgnoreDupKeyOption_FailsToUpdateExistingValue_WhenIgnoreDupKeyOptionWasNotSet(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Hash] REBUILD WITH (IGNORE_DUP_KEY = OFF)");
                sql.Execute($"insert into [{Constants.DefaultSchema}].Hash([Key], Field, Value) VALUES (N'some-hash', N'key', N'value1')");

                var exception = Assert.ThrowsAny<DbException>(() =>
                    Commit(x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                    {
                        { "key", "value2" }
                    }), useMicrosoftDataSqlClient, useBatching, options => options.UseIgnoreDupKeyOption = true));

                Assert.Contains("Violation of PRIMARY KEY", exception.Message);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveHash_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.RemoveHash(null), useMicrosoftDataSqlClient, useBatching));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveHash_DoesNotTruncateKey_BeforeUsingIt(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                // Arrange
                Commit(x => x.SetRangeInHash(
                    TooLongTruncatedKey,
                    new Dictionary<string, string> {{ "field", "value" }}), useMicrosoftDataSqlClient, useBatching);

                // Act
                Commit(x => x.RemoveHash(TooLongKey), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var result = sql.Query(
                    $"select [Value] from [{Constants.DefaultSchema}].[Hash] where [Key] = @key",
                    new { key = TooLongTruncatedKey }).Single();

                Assert.Equal("value", result.Value);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveHash_RemovesAllHashRecords(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                // Arrange
                Commit(x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    { "Key1", "Value1" },
                    { "Key2", "Value2" }
                }), useMicrosoftDataSqlClient, useBatching);

                // Act
                Commit(x => x.RemoveHash("some-hash"), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var count = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].Hash").Single();
                Assert.Equal(0, count);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddRangeToSet_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.AddRangeToSet(null, new List<string>()), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddRangeToSet_ThrowsAnException_WhenKeyIsTooLong(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.ThrowsAny<DbException>(() =>
                    Commit(x => x.AddRangeToSet(
                        TooLongKey,
                        new List<string> { "field" }), useMicrosoftDataSqlClient, useBatching));

                Assert.Contains("data would be truncated", exception.Message);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddRangeToSet_ThrowsAnException_WhenItemsValueIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.AddRangeToSet("my-set", null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("items", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddRangeToSet_AddsAllItems_ToAGivenSet(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var items = new List<string> { "1", "2", "3" };

                Commit(x => x.AddRangeToSet("my-set", items), useMicrosoftDataSqlClient, useBatching);

                var records = sql.Query<string>($"select [Value] from [{Constants.DefaultSchema}].[Set] where [Key] = N'my-set'");
                Assert.Equal(items, records);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddRangeToSet_DoesNotFailWithException_WhenIgnoreDupKeyOptionIsSet(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            try
            {
                UseConnection(sql =>
                {
                    sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Set] REBUILD WITH (IGNORE_DUP_KEY = ON)");
                    sql.Execute($@"insert into [{Constants.DefaultSchema}].[Set] ([Key], Value, Score) VALUES
(N'my-set', N'2', 1.2),
(N'my-set', N'3', 1.2),
(N'my-set', N'4', 1.2)");

                    var items = new List<string> { "1", "2", "3" };

                    Commit(x => x.AddRangeToSet("my-set", items), useMicrosoftDataSqlClient, useBatching);

                    var records = sql.Query<string>($"select [Value] from [{Constants.DefaultSchema}].[Set] where [Key] = N'my-set'");
                    Assert.Equal(new List<string> { "1", "2", "3", "4" }, records);
                }, useMicrosoftDataSqlClient);
            }
            finally
            {
                UseConnection(sql => sql.Execute($"ALTER TABLE [{Constants.DefaultSchema}].[Set] REBUILD WITH (IGNORE_DUP_KEY = OFF)"), useMicrosoftDataSqlClient);
            }
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveSet_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.RemoveSet(null), useMicrosoftDataSqlClient, useBatching));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveSet_DoesNotTruncateKey_BeforeUsingIt(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                // Arrange
                Commit(x => x.AddToSet(TooLongTruncatedKey, "value"), useMicrosoftDataSqlClient, useBatching);

                // Act
                Commit(x => x.RemoveSet(TooLongKey), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var result = sql.Query(
                    $"select [Value] from [{Constants.DefaultSchema}].[Set] where [Key] = @key",
                    new { key = TooLongTruncatedKey }).Single();

                Assert.Equal("value", result.Value);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void RemoveSet_RemovesASet_WithAGivenKey(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].[Set] ([Key], [Value], [Score]) values (@key, @value, 0.0)";

            UseConnection(sql =>
            {
                sql.Execute(arrangeSql, new []
                {
                    new { key = "set-1", value = "1" },
                    new { key = "set-2", value = "1" }
                });

                Commit(x => x.RemoveSet("set-1"), useMicrosoftDataSqlClient, useBatching);

                var record = sql.Query($"select * from [{Constants.DefaultSchema}].[Set]").Single();
                Assert.Equal("set-2", record.Key);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void ExpireHash_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.ExpireHash(null, TimeSpan.FromMinutes(5)), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void ExpireHash_DoesNotTruncateKey_BeforeUsingIt(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                // Arrange
                Commit(x => x.SetRangeInHash(
                    TooLongTruncatedKey,
                    new Dictionary<string, string> {{ "field", "value" }}), useMicrosoftDataSqlClient, useBatching);

                // Act
                Commit(x => x.ExpireHash(TooLongKey, TimeSpan.FromHours(1)), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var result = sql.Query(
                    $"select [ExpireAt] from [{Constants.DefaultSchema}].[Hash] where [Key] = @key",
                    new { key = TooLongTruncatedKey }).Single();

                Assert.Null(result.ExpireAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void ExpireHash_SetsExpirationTimeOnAHash_WithGivenKey(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Hash ([Key], [Field])
values (@key, @field)";

            UseConnection(sql =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "hash-1", field = "field" },
                    new { key = "hash-2", field = "field" }
                });

                // Act
                Commit(x => x.ExpireHash("hash-1", TimeSpan.FromMinutes(60)), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var records = sql.Query($"select * from [{Constants.DefaultSchema}].Hash").ToDictionary(x => (string)x.Key, x => (DateTime?)x.ExpireAt);
                Assert.True(DateTime.UtcNow.AddMinutes(59) < records["hash-1"]);
                Assert.True(records["hash-1"] < DateTime.UtcNow.AddMinutes(61));
                Assert.Null(records["hash-2"]);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void ExpireSet_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.ExpireSet(null, TimeSpan.FromSeconds(45)), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void ExpireSet_DoesNotTruncateKey_BeforeUsingIt(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                // Arrange
                Commit(x => x.AddToSet(TooLongTruncatedKey, "value"), useMicrosoftDataSqlClient, useBatching);

                // Act
                Commit(x => x.ExpireSet(TooLongKey, TimeSpan.FromHours(1)), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var result = sql.Query(
                    $"select [ExpireAt] from [{Constants.DefaultSchema}].[Set] where [Key] = @key",
                    new { key = TooLongTruncatedKey }).Single();

                Assert.Null(result.ExpireAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void ExpireSet_SetsExpirationTime_OnASet_WithGivenKey(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].[Set] ([Key], [Value], [Score])
values (@key, @value, 0.0)";

            UseConnection(sql =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "set-1", value = "1" },
                    new { key = "set-2", value = "1" }
                });

                // Act
                Commit(x => x.ExpireSet("set-1", TimeSpan.FromMinutes(60)), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var records = sql.Query($"select * from [{Constants.DefaultSchema}].[Set]").ToDictionary(x => (string)x.Key, x => (DateTime?)x.ExpireAt);
                Assert.True(DateTime.UtcNow.AddMinutes(59) < records["set-1"]);
                Assert.True(records["set-1"] < DateTime.UtcNow.AddMinutes(61));
                Assert.Null(records["set-2"]);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void ExpireList_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.ExpireList(null, TimeSpan.FromSeconds(45)), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void ExpireList_DoesNotTruncateKey_BeforeUsingIt(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                // Arrange
                Commit(x => x.InsertToList(TooLongTruncatedKey, "value"), useMicrosoftDataSqlClient, useBatching);

                // Act
                Commit(x => x.ExpireList(TooLongKey, TimeSpan.FromHours(1)), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var result = sql.Query(
                    $"select [ExpireAt] from [{Constants.DefaultSchema}].[List] where [Key] = @key",
                    new { key = TooLongTruncatedKey }).Single();

                Assert.Null(result.ExpireAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void ExpireList_SetsExpirationTime_OnAList_WithGivenKey(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].[List] ([Key]) values (@key)";

            UseConnection(sql =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "list-1", value = "1" },
                    new { key = "list-2", value = "1" }
                });

                // Act
                Commit(x => x.ExpireList("list-1", TimeSpan.FromMinutes(60)), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var records = sql.Query($"select * from [{Constants.DefaultSchema}].[List]").ToDictionary(x => (string)x.Key, x => (DateTime?)x.ExpireAt);
                Assert.True(DateTime.UtcNow.AddMinutes(59) < records["list-1"]);
                Assert.True(records["list-1"] < DateTime.UtcNow.AddMinutes(61));
                Assert.Null(records["list-2"]);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void PersistHash_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.PersistHash(null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void PersistHash_DoesNotTruncateKey_BeforeUsingIt(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                // Arrange
                Commit(x =>
                {
                    x.SetRangeInHash(TooLongTruncatedKey, new Dictionary<string, string> { { "field", "value" } });
                    x.ExpireHash(TooLongTruncatedKey, TimeSpan.FromHours(1));
                }, useMicrosoftDataSqlClient, useBatching);

                // Act
                Commit(x => x.PersistHash(TooLongKey), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var result = sql.Query(
                    $"select [ExpireAt] from [{Constants.DefaultSchema}].[Hash] where [Key] = @key",
                    new { key = TooLongTruncatedKey }).Single();

                Assert.NotNull(result.ExpireAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void PersistHash_ClearsExpirationTime_OnAGivenHash(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].Hash ([Key], [Field], [ExpireAt])
values (@key, @field, @expireAt)";

            UseConnection(sql =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "hash-1", field = "field", expireAt = DateTime.UtcNow.AddDays(1) },
                    new { key = "hash-2", field = "field", expireAt = DateTime.UtcNow.AddDays(1) }
                });

                // Act
                Commit(x => x.PersistHash("hash-1"), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var records = sql.Query($"select * from [{Constants.DefaultSchema}].Hash").ToDictionary(x => (string)x.Key, x => (DateTime?)x.ExpireAt);
                Assert.Null(records["hash-1"]);
                Assert.NotNull(records["hash-2"]);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void PersistSet_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.PersistSet(null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void PersistSet_DoesNotTruncateKey_BeforeUsingIt(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                // Arrange
                Commit(x =>
                {
                    x.AddToSet(TooLongTruncatedKey, "value");
                    x.ExpireSet(TooLongTruncatedKey, TimeSpan.FromHours(1));
                }, useMicrosoftDataSqlClient, useBatching);

                // Act
                Commit(x => x.PersistSet(TooLongKey), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var result = sql.Query(
                    $"select [ExpireAt] from [{Constants.DefaultSchema}].[Set] where [Key] = @key",
                    new { key = TooLongTruncatedKey }).Single();

                Assert.NotNull(result.ExpireAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void PersistSet_ClearsExpirationTime_OnAGivenHash(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].[Set] ([Key], [Value], [ExpireAt], [Score])
values (@key, @value, @expireAt, 0.0)";

            UseConnection(sql =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "set-1", value = "1", expireAt = DateTime.UtcNow.AddDays(1) },
                    new { key = "set-2", value = "1", expireAt = DateTime.UtcNow.AddDays(1) }
                });

                // Act
                Commit(x => x.PersistSet("set-1"), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var records = sql.Query($"select * from [{Constants.DefaultSchema}].[Set]").ToDictionary(x => (string)x.Key, x => (DateTime?)x.ExpireAt);
                Assert.Null(records["set-1"]);
                Assert.NotNull(records["set-2"]);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void PersistList_ThrowsAnException_WhenKeyIsNull(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(x => x.PersistList(null), useMicrosoftDataSqlClient, useBatching));

                Assert.Equal("key", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void PersistList_DoesNotTruncateKey_BeforeUsingIt(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                // Arrange
                Commit(x =>
                {
                    x.InsertToList(TooLongTruncatedKey, "value");
                    x.ExpireList(TooLongTruncatedKey, TimeSpan.FromHours(1));
                }, useMicrosoftDataSqlClient, useBatching);

                // Act
                Commit(x => x.PersistList(TooLongKey), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var result = sql.Query(
                    $"select [ExpireAt] from [{Constants.DefaultSchema}].[List] where [Key] = @key",
                    new { key = TooLongTruncatedKey }).Single();

                Assert.NotNull(result.ExpireAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void PersistList_ClearsExpirationTime_OnAGivenHash(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].[List] ([Key], [ExpireAt])
values (@key, @expireAt)";

            UseConnection(sql =>
            {
                // Arrange
                sql.Execute(arrangeSql, new[]
                {
                    new { key = "list-1", expireAt = DateTime.UtcNow.AddDays(1) },
                    new { key = "list-2", expireAt = DateTime.UtcNow.AddDays(1) }
                });

                // Act
                Commit(x => x.PersistList("list-1"), useMicrosoftDataSqlClient, useBatching);

                // Assert
                var records = sql.Query($"select * from [{Constants.DefaultSchema}].[List]").ToDictionary(x => (string)x.Key, x => (DateTime?)x.ExpireAt);
                Assert.Null(records["list-1"]);
                Assert.NotNull(records["list-2"]);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void InsertToList_HandlesListIdCanExceedInt32Max(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                sql.Query($"DBCC CHECKIDENT('[{Constants.DefaultSchema}].List', RESEED, {int.MaxValue + 1L});");

                Commit(x => x.InsertToList("my-key", "my-value"), useMicrosoftDataSqlClient, useBatching);

                var record = sql.Query($"select * from [{Constants.DefaultSchema}].List").Single();

                Assert.True(int.MaxValue < record.Id);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void ExpireJob_SetsJobExpirationData_WhenJobIdIsLongValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
SET IDENTITY_INSERT [{Constants.DefaultSchema}].Job ON
insert into [{Constants.DefaultSchema}].Job (Id, InvocationData, Arguments, CreatedAt)
values (@jobId, '', '', getutcdate())";

            UseConnection(sql =>
            {
                sql.Query(
                    arrangeSql,
                    new { jobId = int.MaxValue + 1L });

                Commit(x => x.ExpireJob((int.MaxValue + 1L).ToString(), TimeSpan.FromDays(1)), useMicrosoftDataSqlClient, useBatching);

                var job = GetTestJob(sql, (int.MaxValue + 1L).ToString());
                Assert.True(DateTime.UtcNow.AddMinutes(-1) < job.ExpireAt && job.ExpireAt <= DateTime.UtcNow.AddDays(2));
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void PersistJob_ClearsTheJobExpirationData_WhenJobIdIsLongValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
SET IDENTITY_INSERT [{Constants.DefaultSchema}].Job ON
insert into [{Constants.DefaultSchema}].Job (Id, InvocationData, Arguments, CreatedAt, ExpireAt)
values (@jobId, '', '', getutcdate(), getutcdate())";

            UseConnection(sql =>
            {
                sql.Query(
                    arrangeSql,
                    new { jobId = int.MaxValue + 1L });

                Commit(x => x.PersistJob((int.MaxValue + 1L).ToString()), useMicrosoftDataSqlClient, useBatching);

                var job = GetTestJob(sql, (int.MaxValue + 1L).ToString());
                Assert.Null(job.ExpireAt);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void SetJobState_WorksCorrect_WhenJobIdIsLongValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
SET IDENTITY_INSERT [{Constants.DefaultSchema}].Job ON
insert into [{Constants.DefaultSchema}].Job (Id, InvocationData, Arguments, CreatedAt)
values (@jobId, '', '', getutcdate())";

            UseConnection(sql =>
            {
                sql.Query(
                    arrangeSql,
                    new { jobId = int.MaxValue + 1L });

                var state = new Mock<IState>();
                state.Setup(x => x.Name).Returns("State");

                Commit(x => x.SetJobState((int.MaxValue + 1L).ToString(), state.Object), useMicrosoftDataSqlClient, useBatching);
                var job = GetTestJob(sql, (int.MaxValue + 1L).ToString());

                var jobState = sql.Query($"select * from [{Constants.DefaultSchema}].State").Single();
                Assert.Equal(int.MaxValue + 1L, jobState.JobId);
                Assert.Equal(job.StateId, jobState.Id);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false, false), InlineData(false, true)]
        [InlineData(true, false), InlineData(true, true)]
        public void AddJobState_AddsAState_WhenJobIdIsLongValue(bool useBatching, bool useMicrosoftDataSqlClient)
        {
            var arrangeSql = $@"
SET IDENTITY_INSERT [{Constants.DefaultSchema}].Job ON
insert into [{Constants.DefaultSchema}].Job (Id, InvocationData, Arguments, CreatedAt)
values (@jobId, '', '', getutcdate())";

            UseConnection(sql =>
            {
                sql.Query(
                   arrangeSql,
                   new { jobId = int.MaxValue + 1L });

                var state = new Mock<IState>();
                state.Setup(x => x.Name).Returns("State");

                Commit(x => x.AddJobState((int.MaxValue + 1L).ToString(), state.Object), useMicrosoftDataSqlClient, useBatching);

                var jobState = sql.Query($"select * from [{Constants.DefaultSchema}].State").Single();

                Assert.Equal(int.MaxValue + 1L, jobState.JobId);
            }, useMicrosoftDataSqlClient);
        }

        private static void UseConnection(Action<DbConnection> action, bool useMicrosoftDataSqlClient)
        {
            using (var connection = ConnectionUtils.CreateConnection(useMicrosoftDataSqlClient))
            {
                action(connection);
            }
        }

        private void Commit(
            Action<SqlServerWriteOnlyTransaction> action,
            bool useMicrosoftDataSqlClient,
            bool useBatching,
            Action<SqlServerStorageOptions> optionsAction = null)
        {
            var options = new SqlServerStorageOptions { CommandBatchMaxTimeout = useBatching ? TimeSpan.FromMinutes(1) : (TimeSpan?) null };
            optionsAction?.Invoke(options);

            var storage = new Mock<SqlServerStorage>((Func<DbConnection>)(() => ConnectionUtils.CreateConnection(useMicrosoftDataSqlClient)), options);
            storage.Setup(x => x.QueueProviders).Returns(_queueProviders);

            using (var transaction = new SqlServerWriteOnlyTransaction(storage.Object, () => null))
            {
                action(transaction);
                transaction.Commit();
            }
        }
    }
}
