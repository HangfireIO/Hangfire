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
                    () => Commit(sql, x => x.ExpireJob(null, TimeSpan.Zero), useBatching));

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

                Commit(sql, x => x.ExpireJob(jobId, TimeSpan.FromHours(24)), useBatching);

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
                    () => Commit(sql, x => x.PersistJob(null), useBatching));

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

                Commit(sql, x => x.PersistJob(jobId), useBatching);

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
                    () => Commit(sql, x => x.SetJobState(null, new Mock<IState>().Object), useBatching));

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
                    () => Commit(sql, x => x.SetJobState("my-job", null), useBatching));

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

                Commit(sql, x => x.SetJobState(jobId, state.Object), useBatching);

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

                Commit(sql, x => x.SetJobState(jobId, state.Object), useBatching);

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
                    () => Commit(sql, x => x.AddJobState(null, new Mock<IState>().Object), useBatching));

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
                    () => Commit(sql, x => x.AddJobState("my-job", null), useBatching));

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

                Commit(sql, x => x.AddJobState(jobId, state.Object), useBatching);

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

                Commit(sql, x => x.AddJobState(jobId, state.Object), useBatching);

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
                    () => Commit(sql, x => x.AddToQueue(null, "my-job"), useBatching));

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
                    () => Commit(sql, x => x.AddToQueue("my-queue", null), useBatching));

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
                Commit(sql, x => x.AddToQueue("default", "1"), useBatching);

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
                Commit(sql, x => x.AddToQueue("default", "1"), useBatching);

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
                    () => Commit(sql, x => x.IncrementCounter(null), useBatching));

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
                    Commit(sql, x => x.IncrementCounter(TooLongKey), useBatching));

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
                Commit(sql, x => x.IncrementCounter("my-key"), useBatching);

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
                    () => Commit(sql, x => x.IncrementCounter(null, TimeSpan.FromHours(1)), useBatching));

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
                    Commit(sql, x => x.IncrementCounter(TooLongKey, TimeSpan.FromHours(1)), useBatching));

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
                Commit(sql, x => x.IncrementCounter("my-key", TimeSpan.FromDays(1)), useBatching);

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
                Commit(sql, x =>
                {
                    x.IncrementCounter("my-key");
                    x.IncrementCounter("my-key");
                }, useBatching);

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
                    () => Commit(sql, x => x.DecrementCounter(null), useBatching));

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
                    Commit(sql, x => x.DecrementCounter(TooLongKey), useBatching));

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
                Commit(sql, x => x.DecrementCounter("my-key"), useBatching);

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
                    () => Commit(sql, x => x.DecrementCounter(null, TimeSpan.FromHours(1)), useBatching));

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
                    Commit(sql, x => x.DecrementCounter(TooLongKey, TimeSpan.FromHours(1)), useBatching));

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
                Commit(sql, x => x.DecrementCounter("my-key", TimeSpan.FromDays(1)), useBatching);

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
                Commit(sql, x =>
                {
                    x.DecrementCounter("my-key");
                    x.DecrementCounter("my-key");
                }, useBatching);

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
                    () => Commit(sql, x => x.AddToSet(null, "value"), useBatching));

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
                    () => Commit(sql, x => x.AddToSet("my-set", null), useBatching));

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
                    Commit(sql, x => x.AddToSet(TooLongKey, "value"), useBatching));

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
                Commit(sql, x => x.AddToSet("my-key", "my-value"), useBatching);

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
                Commit(sql, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "another-value");
                }, useBatching);

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
                Commit(sql, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "my-value");
                }, useBatching);

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
                    () => Commit(sql, x => x.AddToSet(null, "value", 1.2D), useBatching));

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
                    () => Commit(sql, x => x.AddToSet("my-set", null, 1.2D), useBatching));

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
                    Commit(sql, x => x.AddToSet(TooLongKey, "value", 1.2D), useBatching));

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
                Commit(sql, x => x.AddToSet("my-key", "my-value", 3.2), useBatching);

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
                Commit(sql, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "my-value", 3.2);
                }, useBatching);

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

                    Commit(sql, x =>
                        x.AddToSet("my-key","my-value", 3.2),
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

                    Commit(sql, x => 
                        x.AddToSet("my-key1", "value1", 2.3), 
                        useBatching, options => options.UseIgnoreDupKeyOption = true);

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
                    Commit(sql, x => 
                        x.AddToSet("key1", "value1"),
                    useBatching, options => options.UseIgnoreDupKeyOption = true));

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
                    () => Commit(sql, x => x.RemoveFromSet(null, "value"), useBatching));

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
                    () => Commit(sql, x => x.RemoveFromSet("my-set", null), useBatching));

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
                Commit(sql, x => x.AddToSet(TooLongTruncatedKey, "value"), useBatching);

                // Act
                Commit(sql, x => x.RemoveFromSet(TooLongKey, "value"), useBatching);

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
                Commit(sql, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("my-key", "my-value");
                }, useBatching);

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
                Commit(sql, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("my-key", "different-value");
                }, useBatching);

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
                Commit(sql, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("different-key", "my-value");
                }, useBatching);

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
                    () => Commit(sql, x => x.InsertToList(null, "value"), useBatching));

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
                    () => Commit(sql, x => x.InsertToList("my-list", null), useBatching));

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
                    Commit(sql, x => x.InsertToList(TooLongKey, "value"), useBatching));

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
                Commit(sql, x => x.InsertToList("my-key", "my-value"), useBatching);

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
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.InsertToList("my-key", "my-value");
                }, useBatching);

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
                    () => Commit(sql, x => x.RemoveFromList(null, "value"), useBatching));

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
                    () => Commit(sql, x => x.RemoveFromList("my-list", null), useBatching));

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
                Commit(sql, x => x.InsertToList(TooLongTruncatedKey, "value"), useBatching);

                // Act
                Commit(sql, x => x.RemoveFromList(TooLongKey, "value"), useBatching);

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
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("my-key", "my-value");
                }, useBatching);

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
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("my-key", "different-value");
                }, useBatching);

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
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("different-key", "my-value");
                }, useBatching);

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
                    () => Commit(sql, x => x.TrimList(null, 0, 1), useBatching));

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
                Commit(sql, x => x.InsertToList(TooLongTruncatedKey, "value"), useBatching);

                // Act
                Commit(sql, x => x.TrimList(TooLongKey, 1, 2), useBatching);

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
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.InsertToList("my-key", "1");
                    x.InsertToList("my-key", "2");
                    x.InsertToList("my-key", "3");
                    x.TrimList("my-key", 1, 2);
                }, useBatching);

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
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.InsertToList("my-key", "1");
                    x.InsertToList("my-key", "2");
                    x.TrimList("my-key", 1, 100);
                }, useBatching);

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
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("my-key", 1, 100);
                }, useBatching);

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
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("my-key", 1, 0);
                }, useBatching);

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
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("another-key", 1, 0);
                }, useBatching);

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
                    () => Commit(sql, x => x.SetRangeInHash(null, new Dictionary<string, string>()), useBatching));

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
                    () => Commit(sql, x => x.SetRangeInHash("some-hash", null), useBatching));

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
                    Commit(sql, x => x.SetRangeInHash(
                        TooLongKey,
                        new Dictionary<string, string> { { "field", "value" } }), useBatching));

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
                Commit(sql, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    { "Key1", "Value1" },
                    { "Key2", "Value2" }
                }), useBatching);

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
                Commit(sql, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    { "Key1", null }
                }), useBatching);

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

                    Commit(sql, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                    {
                        { "key", "value" }
                    }), useBatching, options => options.UseIgnoreDupKeyOption = true);

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

                    Commit(sql, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                    {
                        { "key1", "value2" }
                    }), useBatching, options => options.UseIgnoreDupKeyOption = true);

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
                    Commit(sql, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                    {
                        { "key", "value2" }
                    }), useBatching, options => options.UseIgnoreDupKeyOption = true));

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
                    () => Commit(sql, x => x.RemoveHash(null), useBatching));
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
                Commit(sql, x => x.SetRangeInHash(
                    TooLongTruncatedKey,
                    new Dictionary<string, string> {{ "field", "value" }}), useBatching);

                // Act
                Commit(sql, x => x.RemoveHash(TooLongKey), useBatching);

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
                Commit(sql, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    { "Key1", "Value1" },
                    { "Key2", "Value2" }
                }), useBatching);

                // Act
                Commit(sql, x => x.RemoveHash("some-hash"), useBatching);

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
                    () => Commit(sql, x => x.AddRangeToSet(null, new List<string>()), useBatching));

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
                    Commit(sql, x => x.AddRangeToSet(
                        TooLongKey,
                        new List<string> { "field" }), useBatching));

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
                    () => Commit(sql, x => x.AddRangeToSet("my-set", null), useBatching));

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

                Commit(sql, x => x.AddRangeToSet("my-set", items), useBatching);

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

                    Commit(sql, x => x.AddRangeToSet("my-set", items), useBatching);

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
                    () => Commit(sql, x => x.RemoveSet(null), useBatching));
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
                Commit(sql, x => x.AddToSet(TooLongTruncatedKey, "value"), useBatching);

                // Act
                Commit(sql, x => x.RemoveSet(TooLongKey), useBatching);

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

                Commit(sql, x => x.RemoveSet("set-1"), useBatching);

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
                    () => Commit(sql, x => x.ExpireHash(null, TimeSpan.FromMinutes(5)), useBatching));

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
                Commit(sql, x => x.SetRangeInHash(
                    TooLongTruncatedKey,
                    new Dictionary<string, string> {{ "field", "value" }}), useBatching);

                // Act
                Commit(sql, x => x.ExpireHash(TooLongKey, TimeSpan.FromHours(1)), useBatching);

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
                Commit(sql, x => x.ExpireHash("hash-1", TimeSpan.FromMinutes(60)), useBatching);

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
                    () => Commit(sql, x => x.ExpireSet(null, TimeSpan.FromSeconds(45)), useBatching));

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
                Commit(sql, x => x.AddToSet(TooLongTruncatedKey, "value"), useBatching);

                // Act
                Commit(sql, x => x.ExpireSet(TooLongKey, TimeSpan.FromHours(1)), useBatching);

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
                Commit(sql, x => x.ExpireSet("set-1", TimeSpan.FromMinutes(60)), useBatching);

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
                    () => Commit(sql, x => x.ExpireList(null, TimeSpan.FromSeconds(45)), useBatching));

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
                Commit(sql, x => x.InsertToList(TooLongTruncatedKey, "value"), useBatching);

                // Act
                Commit(sql, x => x.ExpireList(TooLongKey, TimeSpan.FromHours(1)), useBatching);

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
                Commit(sql, x => x.ExpireList("list-1", TimeSpan.FromMinutes(60)), useBatching);

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
                    () => Commit(sql, x => x.PersistHash(null), useBatching));

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
                Commit(sql, x =>
                {
                    x.SetRangeInHash(TooLongTruncatedKey, new Dictionary<string, string> { { "field", "value" } });
                    x.ExpireHash(TooLongTruncatedKey, TimeSpan.FromHours(1));
                }, useBatching);

                // Act
                Commit(sql, x => x.PersistHash(TooLongKey), useBatching);

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
                Commit(sql, x => x.PersistHash("hash-1"), useBatching);

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
                    () => Commit(sql, x => x.PersistSet(null), useBatching));

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
                Commit(sql, x =>
                {
                    x.AddToSet(TooLongTruncatedKey, "value");
                    x.ExpireSet(TooLongTruncatedKey, TimeSpan.FromHours(1));
                }, useBatching);

                // Act
                Commit(sql, x => x.PersistSet(TooLongKey), useBatching);

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
                Commit(sql, x => x.PersistSet("set-1"), useBatching);

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
                    () => Commit(sql, x => x.PersistList(null), useBatching));

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
                Commit(sql, x =>
                {
                    x.InsertToList(TooLongTruncatedKey, "value");
                    x.ExpireList(TooLongTruncatedKey, TimeSpan.FromHours(1));
                }, useBatching);

                // Act
                Commit(sql, x => x.PersistList(TooLongKey), useBatching);

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
                Commit(sql, x => x.PersistList("list-1"), useBatching);

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

                Commit(sql, x => x.InsertToList("my-key", "my-value"), useBatching);

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

                Commit(sql, x => x.ExpireJob((int.MaxValue + 1L).ToString(), TimeSpan.FromDays(1)), useBatching);

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

                Commit(sql, x => x.PersistJob((int.MaxValue + 1L).ToString()), useBatching);

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

                Commit(sql, x => x.SetJobState((int.MaxValue + 1L).ToString(), state.Object), useBatching);
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

                Commit(sql, x => x.AddJobState((int.MaxValue + 1L).ToString(), state.Object), useBatching);

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
            DbConnection connection,
            Action<SqlServerWriteOnlyTransaction> action,
            bool useBatching,
            Action<SqlServerStorageOptions> optionsAction = null)
        {
            var options = new SqlServerStorageOptions { CommandBatchMaxTimeout = useBatching ? TimeSpan.FromMinutes(1) : (TimeSpan?) null };
            optionsAction?.Invoke(options);

            var storage = new Mock<SqlServerStorage>(connection, options);
            storage.Setup(x => x.QueueProviders).Returns(_queueProviders);

            using (var transaction = new SqlServerWriteOnlyTransaction(storage.Object, () => null))
            {
                action(transaction);
                transaction.Commit();
            }
        }
    }
}
