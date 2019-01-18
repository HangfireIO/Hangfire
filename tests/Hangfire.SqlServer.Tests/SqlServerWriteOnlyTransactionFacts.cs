using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Hangfire.States;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerWriteOnlyTransactionFacts
    {
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
        [InlineData(true)]
        [InlineData(false)]
        public void ExpireJob_SetsJobExpirationData(bool useBatching)
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
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
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void PersistJob_ClearsTheJobExpirationData(bool useBatching)
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt, ExpireAt)
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
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void SetJobState_AppendsAStateAndSetItToTheJob(bool useBatching)
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
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

                var jobState = sql.Query("select * from HangFire.State").Single();
                Assert.Equal((string)jobId, jobState.JobId.ToString());
                Assert.Equal("State", jobState.Name);
                Assert.Equal("Reason", jobState.Reason);
                Assert.NotNull(jobState.CreatedAt);
                Assert.Equal("{\"Name\":\"Value\"}", jobState.Data);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void SetJobState_CanBeCalledWithNullReasonAndData(bool useBatching)
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
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

                var jobState = sql.Query("select * from HangFire.State").Single();
                Assert.Equal("State", jobState.Name);
                Assert.Equal(null, jobState.Reason);
                Assert.Equal(null, jobState.Data);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void AddJobState_JustAddsANewRecordInATable(bool useBatching)
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
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

                var jobState = sql.Query("select * from HangFire.State").Single();
                Assert.Equal((string)jobId, jobState.JobId.ToString());
                Assert.Equal("State", jobState.Name);
                Assert.Equal("Reason", jobState.Reason);
                Assert.NotNull(jobState.CreatedAt);
                Assert.Equal("{\"Name\":\"Value\"}", jobState.Data);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void AddJobState_CanBeCalledWithNullReasonAndData(bool useBatching)
        {
            const string arrangeSql = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt)
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

                var jobState = sql.Query("select * from HangFire.State").Single();
                Assert.Equal((string)jobId, jobState.JobId.ToString());
                Assert.Equal("State", jobState.Name);
                Assert.Equal(null, jobState.Reason);
                Assert.NotNull(jobState.CreatedAt);
                Assert.Equal(null, jobState.Data);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void AddToQueue_CallsEnqueue_OnTargetPersistentQueue(bool useBatching)
        {
            var correctJobQueue = new Mock<IPersistentJobQueue>();
            var correctProvider = new Mock<IPersistentJobQueueProvider>();
            correctProvider.Setup(x => x.GetJobQueue())
                .Returns(correctJobQueue.Object);

            _queueProviders.Add(correctProvider.Object, new[] { "default" });

            UseConnection(sql =>
            {
                Commit(sql, x => x.AddToQueue("default", "1"), useBatching);

                correctJobQueue.Verify(x => x.Enqueue(It.IsNotNull<IDbConnection>(), "default", "1"));
            });
        }

        private static dynamic GetTestJob(IDbConnection connection, string jobId)
        {
            return connection
                .Query("select * from HangFire.Job where Id = @id", new { id = jobId })
                .Single();
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void IncrementCounter_AddsRecordToCounterTable_WithPositiveValue(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x => x.IncrementCounter("my-key"), useBatching);

                var record = sql.Query("select * from HangFire.Counter").Single();
                
                Assert.Equal("my-key", record.Key);
                Assert.Equal(1, record.Value);
                Assert.Equal((DateTime?)null, record.ExpireAt);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void IncrementCounter_WithExpiry_AddsARecord_WithExpirationTimeSet(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x => x.IncrementCounter("my-key", TimeSpan.FromDays(1)), useBatching);

                var record = sql.Query("select * from HangFire.Counter").Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(1, record.Value);
                Assert.NotNull(record.ExpireAt);

                var expireAt = (DateTime) record.ExpireAt;

                Assert.True(DateTime.UtcNow.AddHours(23) < expireAt);
                Assert.True(expireAt < DateTime.UtcNow.AddHours(25));
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void IncrementCounter_WithExistingKey_AddsAnotherRecord(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.IncrementCounter("my-key");
                    x.IncrementCounter("my-key");
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.Counter").Single();
                
                Assert.Equal(2, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void DecrementCounter_AddsRecordToCounterTable_WithNegativeValue(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x => x.DecrementCounter("my-key"), useBatching);

                var record = sql.Query("select * from HangFire.Counter").Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(-1, record.Value);
                Assert.Equal((DateTime?)null, record.ExpireAt);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void DecrementCounter_WithExpiry_AddsARecord_WithExpirationTimeSet(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x => x.DecrementCounter("my-key", TimeSpan.FromDays(1)), useBatching);

                var record = sql.Query("select * from HangFire.Counter").Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal(-1, record.Value);
                Assert.NotNull(record.ExpireAt);

                var expireAt = (DateTime)record.ExpireAt;

                Assert.True(DateTime.UtcNow.AddHours(23) < expireAt);
                Assert.True(expireAt < DateTime.UtcNow.AddHours(25));
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void DecrementCounter_WithExistingKey_AddsAnotherRecord(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.DecrementCounter("my-key");
                    x.DecrementCounter("my-key");
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.Counter").Single();

                Assert.Equal(2, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void AddToSet_AddsARecord_IfThereIsNo_SuchKeyAndValue(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x => x.AddToSet("my-key", "my-value"), useBatching);

                var record = sql.Query("select * from HangFire.[Set]").Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
                Assert.Equal(0.0, record.Score, 2);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void AddToSet_AddsARecord_WhenKeyIsExists_ButValuesAreDifferent(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "another-value");
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.[Set]").Single();

                Assert.Equal(2, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void AddToSet_DoesNotAddARecord_WhenBothKeyAndValueAreExist(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "my-value");
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.[Set]").Single();
                
                Assert.Equal(1, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void AddToSet_WithScore_AddsARecordWithScore_WhenBothKeyAndValueAreNotExist(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x => x.AddToSet("my-key", "my-value", 3.2), useBatching);

                var record = sql.Query("select * from HangFire.[Set]").Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
                Assert.Equal(3.2, record.Score, 3);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void AddToSet_WithScore_UpdatesAScore_WhenBothKeyAndValueAreExist(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.AddToSet("my-key", "my-value", 3.2);
                }, useBatching);

                var record = sql.Query("select * from HangFire.[Set]").Single();

                Assert.Equal(3.2, record.Score, 3);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void RemoveFromSet_RemovesARecord_WithGivenKeyAndValue(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("my-key", "my-value");
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.[Set]").Single();

                Assert.Equal(0, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void RemoveFromSet_DoesNotRemoveRecord_WithSameKey_AndDifferentValue(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("my-key", "different-value");
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.[Set]").Single();

                Assert.Equal(1, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void RemoveFromSet_DoesNotRemoveRecord_WithSameValue_AndDifferentKey(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.AddToSet("my-key", "my-value");
                    x.RemoveFromSet("different-key", "my-value");
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.[Set]").Single();

                Assert.Equal(1, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void InsertToList_AddsARecord_WithGivenValues(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x => x.InsertToList("my-key", "my-value"), useBatching);

                var record = sql.Query("select * from HangFire.List").Single();

                Assert.Equal("my-key", record.Key);
                Assert.Equal("my-value", record.Value);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void InsertToList_AddsAnotherRecord_WhenBothKeyAndValueAreExist(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.InsertToList("my-key", "my-value");
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.List").Single();

                Assert.Equal(2, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void RemoveFromList_RemovesAllRecords_WithGivenKeyAndValue(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("my-key", "my-value");
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.List").Single();

                Assert.Equal(0, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void RemoveFromList_DoesNotRemoveRecords_WithSameKey_ButDifferentValue(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("my-key", "different-value");
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.List").Single();

                Assert.Equal(1, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void RemoveFromList_DoesNotRemoveRecords_WithSameValue_ButDifferentKey(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "my-value");
                    x.RemoveFromList("different-key", "my-value");
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.List").Single();

                Assert.Equal(1, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void TrimList_TrimsAList_ToASpecifiedRange(bool useBatching)
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

                var records = sql.Query("select * from HangFire.List").ToArray();

                Assert.Equal(2, records.Length);
                Assert.Equal("1", records[0].Value);
                Assert.Equal("2", records[1].Value);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void TrimList_RemovesRecordsToEnd_IfKeepAndingAt_GreaterThanMaxElementIndex(bool useBatching)
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

                var recordCount = sql.Query<int>("select count(*) from HangFire.List").Single();

                Assert.Equal(2, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void TrimList_RemovesAllRecords_WhenStartingFromValue_GreaterThanMaxElementIndex(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("my-key", 1, 100);
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.List").Single();

                Assert.Equal(0, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void TrimList_RemovesAllRecords_IfStartFromGreaterThanEndingAt(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("my-key", 1, 0);
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.List").Single();

                Assert.Equal(0, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void TrimList_RemovesRecords_OnlyOfAGivenKey(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x =>
                {
                    x.InsertToList("my-key", "0");
                    x.TrimList("another-key", 1, 0);
                }, useBatching);

                var recordCount = sql.Query<int>("select count(*) from HangFire.List").Single();

                Assert.Equal(1, recordCount);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void SetRangeInHash_ThrowsAnException_WhenKeyIsNull(bool useBatching)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(sql, x => x.SetRangeInHash(null, new Dictionary<string, string>()), useBatching));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void SetRangeInHash_ThrowsAnException_WhenKeyValuePairsArgumentIsNull(bool useBatching)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(sql, x => x.SetRangeInHash("some-hash", null), useBatching));

                Assert.Equal("keyValuePairs", exception.ParamName);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void SetRangeInHash_MergesAllRecords(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    { "Key1", "Value1" },
                    { "Key2", "Value2" }
                }), useBatching);

                var result = sql.Query(
                    "select * from HangFire.Hash where [Key] = @key",
                    new { key = "some-hash" })
                    .ToDictionary(x => (string)x.Field, x => (string)x.Value);

                Assert.Equal("Value1", result["Key1"]);
                Assert.Equal("Value2", result["Key2"]);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void SetRangeInHash_CanSetANullValue(bool useBatching)
        {
            UseConnection(sql =>
            {
                Commit(sql, x => x.SetRangeInHash("some-hash", new Dictionary<string, string>
                {
                    { "Key1", null }
                }), useBatching);

                var result = sql.Query(
                        "select * from HangFire.Hash where [Key] = @key",
                        new { key = "some-hash" })
                    .ToDictionary(x => (string)x.Field, x => (string)x.Value);

                Assert.Equal(null, result["Key1"]);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void RemoveHash_ThrowsAnException_WhenKeyIsNull(bool useBatching)
        {
            UseConnection(sql =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => Commit(sql, x => x.RemoveHash(null), useBatching));
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void RemoveHash_RemovesAllHashRecords(bool useBatching)
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
                var count = sql.Query<int>("select count(*) from HangFire.Hash").Single();
                Assert.Equal(0, count);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void AddRangeToSet_ThrowsAnException_WhenKeyIsNull(bool useBatching)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(sql, x => x.AddRangeToSet(null, new List<string>()), useBatching));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void AddRangeToSet_ThrowsAnException_WhenItemsValueIsNull(bool useBatching)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(sql, x => x.AddRangeToSet("my-set", null), useBatching));

                Assert.Equal("items", exception.ParamName);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void AddRangeToSet_AddsAllItems_ToAGivenSet(bool useBatching)
        {
            UseConnection(sql =>
            {
                var items = new List<string> { "1", "2", "3" };

                Commit(sql, x => x.AddRangeToSet("my-set", items), useBatching);

                var records = sql.Query<string>(@"select [Value] from HangFire.[Set] where [Key] = N'my-set'");
                Assert.Equal(items, records);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void RemoveSet_ThrowsAnException_WhenKeyIsNull(bool useBatching)
        {
            UseConnection(sql =>
            {
                Assert.Throws<ArgumentNullException>(
                    () => Commit(sql, x => x.RemoveSet(null), useBatching));
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void RemoveSet_RemovesASet_WithAGivenKey(bool useBatching)
        {
            const string arrangeSql = @"
insert into HangFire.[Set] ([Key], [Value], [Score]) values (@key, @value, 0.0)";

            UseConnection(sql =>
            {
                sql.Execute(arrangeSql, new []
                {
                    new { key = "set-1", value = "1" },
                    new { key = "set-2", value = "1" }
                });

                Commit(sql, x => x.RemoveSet("set-1"), useBatching);

                var record = sql.Query("select * from HangFire.[Set]").Single();
                Assert.Equal("set-2", record.Key);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void ExpireHash_ThrowsAnException_WhenKeyIsNull(bool useBatching)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(sql, x => x.ExpireHash(null, TimeSpan.FromMinutes(5)), useBatching));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void ExpireHash_SetsExpirationTimeOnAHash_WithGivenKey(bool useBatching)
        {
            const string arrangeSql = @"
insert into HangFire.Hash ([Key], [Field])
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
                var records = sql.Query("select * from HangFire.Hash").ToDictionary(x => (string)x.Key, x => (DateTime?)x.ExpireAt);
                Assert.True(DateTime.UtcNow.AddMinutes(59) < records["hash-1"]);
                Assert.True(records["hash-1"] < DateTime.UtcNow.AddMinutes(61));
                Assert.Null(records["hash-2"]);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void ExpireSet_ThrowsAnException_WhenKeyIsNull(bool useBatching)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(sql, x => x.ExpireSet(null, TimeSpan.FromSeconds(45)), useBatching));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void ExpireSet_SetsExpirationTime_OnASet_WithGivenKey(bool useBatching)
        {
            const string arrangeSql = @"
insert into HangFire.[Set] ([Key], [Value], [Score])
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
                var records = sql.Query("select * from HangFire.[Set]").ToDictionary(x => (string)x.Key, x => (DateTime?)x.ExpireAt);
                Assert.True(DateTime.UtcNow.AddMinutes(59) < records["set-1"]);
                Assert.True(records["set-1"] < DateTime.UtcNow.AddMinutes(61));
                Assert.Null(records["set-2"]);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void ExpireList_ThrowsAnException_WhenKeyIsNull(bool useBatching)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(sql, x => x.ExpireList(null, TimeSpan.FromSeconds(45)), useBatching));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void ExpireList_SetsExpirationTime_OnAList_WithGivenKey(bool useBatching)
        {
            const string arrangeSql = @"
insert into HangFire.[List] ([Key]) values (@key)";

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
                var records = sql.Query("select * from HangFire.[List]").ToDictionary(x => (string)x.Key, x => (DateTime?)x.ExpireAt);
                Assert.True(DateTime.UtcNow.AddMinutes(59) < records["list-1"]);
                Assert.True(records["list-1"] < DateTime.UtcNow.AddMinutes(61));
                Assert.Null(records["list-2"]);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void PersistHash_ThrowsAnException_WhenKeyIsNull(bool useBatching)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(sql, x => x.PersistHash(null), useBatching));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void PersistHash_ClearsExpirationTime_OnAGivenHash(bool useBatching)
        {
            const string arrangeSql = @"
insert into HangFire.Hash ([Key], [Field], [ExpireAt])
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
                var records = sql.Query("select * from HangFire.Hash").ToDictionary(x => (string)x.Key, x => (DateTime?)x.ExpireAt);
                Assert.Null(records["hash-1"]);
                Assert.NotNull(records["hash-2"]);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void PersistSet_ThrowsAnException_WhenKeyIsNull(bool useBatching)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(sql, x => x.PersistSet(null), useBatching));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void PersistSet_ClearsExpirationTime_OnAGivenHash(bool useBatching)
        {
            const string arrangeSql = @"
insert into HangFire.[Set] ([Key], [Value], [ExpireAt], [Score])
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
                var records = sql.Query("select * from HangFire.[Set]").ToDictionary(x => (string)x.Key, x => (DateTime?)x.ExpireAt);
                Assert.Null(records["set-1"]);
                Assert.NotNull(records["set-2"]);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void PersistList_ThrowsAnException_WhenKeyIsNull(bool useBatching)
        {
            UseConnection(sql =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => Commit(sql, x => x.PersistList(null), useBatching));

                Assert.Equal("key", exception.ParamName);
            });
        }

        [Theory, CleanDatabase]
        [InlineData(true)]
        [InlineData(false)]
        public void PersistList_ClearsExpirationTime_OnAGivenHash(bool useBatching)
        {
            const string arrangeSql = @"
insert into HangFire.[List] ([Key], [ExpireAt])
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
                var records = sql.Query("select * from HangFire.[List]").ToDictionary(x => (string)x.Key, x => (DateTime?)x.ExpireAt);
                Assert.Null(records["list-1"]);
                Assert.NotNull(records["list-2"]);
            });
        }

        private static void UseConnection(Action<SqlConnection> action)
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                action(connection);
            }
        }

        private void Commit(
            SqlConnection connection,
            Action<SqlServerWriteOnlyTransaction> action,
            bool useBatching)
        {
            var storage = new Mock<SqlServerStorage>(connection, new SqlServerStorageOptions { CommandBatchMaxTimeout = useBatching ? TimeSpan.FromMinutes(1) : (TimeSpan?)null });
            storage.Setup(x => x.QueueProviders).Returns(_queueProviders);

            using (var transaction = new SqlServerWriteOnlyTransaction(storage.Object, () => null))
            {
                action(transaction);
                transaction.Commit();
            }
        }
    }
}
