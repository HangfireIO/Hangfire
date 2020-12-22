extern alias ReferencedDapper;

using System;
using System.Data;
using System.Linq;
using System.Threading;
using ReferencedDapper::Dapper;
using Xunit;
// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerTimeoutJobFacts
    {
        private const string JobId = "id";
        private const string Queue = "queue";
        private static readonly DateTime FetchedAt = DateTime.UtcNow;

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerTimeoutJob(null, 1, JobId, Queue, FetchedAt));

            Assert.Equal("storage", exception.ParamName);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection((sql, storage) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => new SqlServerTimeoutJob(storage, 1, null, Queue, FetchedAt));

                Assert.Equal("jobId", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Ctor_ThrowsAnException_WhenQueueIsNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection((sql, storage) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => new SqlServerTimeoutJob(storage, 1, JobId, null, FetchedAt));

                Assert.Equal("queue", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Ctor_CorrectlySets_AllInstanceProperties(bool useMicrosoftDataSqlClient)
        {
            UseConnection((sql, storage) =>
            {
                using (var fetchedJob = new SqlServerTimeoutJob(storage, 1, JobId, Queue, FetchedAt))
                {
                    Assert.Equal(1, fetchedJob.Id);
                    Assert.Equal(JobId, fetchedJob.JobId);
                    Assert.Equal(Queue, fetchedJob.Queue);
                    Assert.Equal(FetchedAt, fetchedJob.FetchedAt);
                }
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void RemoveFromQueue_ReallyDeletesTheJobFromTheQueue(bool useMicrosoftDataSqlClient)
        {
            UseConnection((sql, storage) =>
            {
                // Arrange
                var id = CreateJobQueueRecord(sql, "1", "default", FetchedAt);
                using (var processingJob = new SqlServerTimeoutJob(storage, id, "1", "default", FetchedAt))
                {
                    processingJob.DisposeTimer();

                    // Act
                    processingJob.RemoveFromQueue();

                    // Assert
                    var count = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].JobQueue").Single();
                    Assert.Equal(0, count);
                }
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void RemoveFromQueue_DoesNotDelete_UnrelatedJobs(bool useMicrosoftDataSqlClient)
        {
            UseConnection((sql, storage) =>
            {
                // Arrange
                CreateJobQueueRecord(sql, "1", "default", FetchedAt);
                CreateJobQueueRecord(sql, "1", "critical", FetchedAt);
                CreateJobQueueRecord(sql, "2", "default", FetchedAt);

                using (var fetchedJob = new SqlServerTimeoutJob(storage, 999, "1", "default", FetchedAt))
                {
                    fetchedJob.DisposeTimer();

                    // Act
                    fetchedJob.RemoveFromQueue();

                    // Assert
                    var count = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].JobQueue").Single();
                    Assert.Equal(3, count);
                }
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Requeue_SetsFetchedAtValueToNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection((sql, storage) =>
            {
                // Arrange
                var id = CreateJobQueueRecord(sql, "1", "default", FetchedAt);
                using (var processingJob = new SqlServerTimeoutJob(storage, id, "1", "default", FetchedAt))
                {
                    processingJob.DisposeTimer();

                    // Act
                    processingJob.Requeue();

                    // Assert
                    var record = sql.Query($"select * from [{Constants.DefaultSchema}].JobQueue").Single();
                    Assert.Null(record.FetchedAt);
                }
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Timer_UpdatesFetchedAtColumn(bool useMicrosoftDataSqlClient)
        {
            UseConnection((sql, storage) =>
            {
                // Arrange
                var id = CreateJobQueueRecord(sql, "1", "default", FetchedAt);
                using (var processingJob = new SqlServerTimeoutJob(storage, id, "1", "default", FetchedAt))
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    processingJob.DisposeTimer();

                    var record = sql.Query($"select * from [{Constants.DefaultSchema}].JobQueue").Single();

                    Assert.NotNull(processingJob.FetchedAt);
                    Assert.Equal<DateTime?>(processingJob.FetchedAt, record.FetchedAt);
                    var now = DateTime.UtcNow;
                    Assert.True(now.AddSeconds(-5) < record.FetchedAt, (now - record.FetchedAt).ToString());
                }
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void RemoveFromQueue_AfterTimer_RemovesJobFromTheQueue(bool useMicrosoftDataSqlClient)
        {
            UseConnection((sql, storage) =>
            {
                // Arrange
                var id = CreateJobQueueRecord(sql, "1", "default", FetchedAt);
                using (var processingJob = new SqlServerTimeoutJob(storage, id, "1", "default", FetchedAt))
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    processingJob.DisposeTimer();

                    // Act
                    processingJob.RemoveFromQueue();

                    // Assert
                    var count = sql.Query<int>($"select count(*) from [{Constants.DefaultSchema}].JobQueue").Single();
                    Assert.Equal(0, count);
                }
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void RequeueQueue_AfterTimer_SetsFetchedAtValueToNull(bool useMicrosoftDataSqlClient)
        {
            UseConnection((sql, storage) =>
            {
                // Arrange
                var id = CreateJobQueueRecord(sql, "1", "default", FetchedAt);
                using (var processingJob = new SqlServerTimeoutJob(storage, id, "1", "default", FetchedAt))
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    processingJob.DisposeTimer();

                    // Act
                    processingJob.Requeue();

                    // Assert
                    var record = sql.Query($"select * from [{Constants.DefaultSchema}].JobQueue").Single();
                    Assert.Null(record.FetchedAt);
                }
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dispose_SetsFetchedAtValueToNull_IfThereWereNoCallsToComplete(bool useMicrosoftDataSqlClient)
        {
            UseConnection((sql, storage) =>
            {
                // Arrange
                var id = CreateJobQueueRecord(sql, "1", "default", FetchedAt);
                using (var processingJob = new SqlServerTimeoutJob(storage, id, "1", "default", FetchedAt))
                {
                    // Act
                    processingJob.Dispose();

                    // Assert
                    var record = sql.Query($"select * from [{Constants.DefaultSchema}].JobQueue").Single();
                    Assert.Null(record.FetchedAt);
                }
            }, useMicrosoftDataSqlClient);
        }

        private static int CreateJobQueueRecord(IDbConnection connection, string jobId, string queue, DateTime? fetchedAt)
        {
            var arrangeSql = $@"
insert into [{Constants.DefaultSchema}].JobQueue (JobId, Queue, FetchedAt)
values (@id, @queue, @fetchedAt);
select scope_identity() as Id";

            return (int)connection.Query(arrangeSql, new { id = jobId, queue, fetchedAt }).Single().Id;
        }

        private static void UseConnection(Action<IDbConnection, SqlServerStorage> action, bool useMicrosoftDataSqlClient)
        {
            using (var connection = ConnectionUtils.CreateConnection(useMicrosoftDataSqlClient))
            {
                var storage = new SqlServerStorage(
                    connection,
                    new SqlServerStorageOptions { SlidingInvisibilityTimeout = TimeSpan.FromSeconds(10) });

                action(connection, storage);
            }
        }
    }
}