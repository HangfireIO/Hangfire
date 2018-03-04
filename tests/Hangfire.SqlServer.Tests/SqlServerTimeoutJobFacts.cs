using System;
using System.Data;
using System.Linq;
using System.Threading;
using Dapper;
using Xunit;
// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerTimeoutJobFacts
    {
        private const string JobId = "id";
        private const string Queue = "queue";

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerTimeoutJob(null, 1, JobId, Queue));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact, CleanDatabase]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            UseConnection((sql, storage) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => new SqlServerTimeoutJob(storage, 1, null, Queue));

                Assert.Equal("jobId", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            UseConnection((sql, storage) =>
            {
                var exception = Assert.Throws<ArgumentNullException>(
                    () => new SqlServerTimeoutJob(storage, 1, JobId, null));

                Assert.Equal("queue", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            UseConnection((sql, storage) =>
            {
                using (var fetchedJob = new SqlServerTimeoutJob(storage, 1, JobId, Queue))
                {
                    Assert.Equal(1, fetchedJob.Id);
                    Assert.Equal(JobId, fetchedJob.JobId);
                    Assert.Equal(Queue, fetchedJob.Queue);
                }
            });
        }

        [Fact, CleanDatabase]
        public void RemoveFromQueue_ReallyDeletesTheJobFromTheQueue()
        {
            UseConnection((sql, storage) =>
            {
                // Arrange
                var id = CreateJobQueueRecord(sql, "1", "default");
                using (var processingJob = new SqlServerTimeoutJob(storage, id, "1", "default"))
                {
                    // Act
                    processingJob.RemoveFromQueue();

                    // Assert
                    var count = sql.Query<int>("select count(*) from HangFire.JobQueue").Single();
                    Assert.Equal(0, count);
                }
            });
        }

        [Fact, CleanDatabase]
        public void RemoveFromQueue_DoesNotDelete_UnrelatedJobs()
        {
            UseConnection((sql, storage) =>
            {
                // Arrange
                CreateJobQueueRecord(sql, "1", "default");
                CreateJobQueueRecord(sql, "1", "critical");
                CreateJobQueueRecord(sql, "2", "default");

                using (var fetchedJob = new SqlServerTimeoutJob(storage, 999, "1", "default"))
                {
                    // Act
                    fetchedJob.RemoveFromQueue();

                    // Assert
                    var count = sql.Query<int>("select count(*) from HangFire.JobQueue").Single();
                    Assert.Equal(3, count);
                }
            });
        }

        [Fact, CleanDatabase]
        public void Requeue_SetsFetchedAtValueToNull()
        {
            UseConnection((sql, storage) =>
            {
                // Arrange
                var id = CreateJobQueueRecord(sql, "1", "default");
                using (var processingJob = new SqlServerTimeoutJob(storage, id, "1", "default"))
                {
                    // Act
                    processingJob.Requeue();

                    // Assert
                    var record = sql.Query("select * from HangFire.JobQueue").Single();
                    Assert.Null(record.FetchedAt);
                }
            });
        }

        [Fact, CleanDatabase]
        public void Timer_UpdatesFetchedAtColumn()
        {
            UseConnection((sql, storage) =>
            {
                // Arrange
                var id = CreateJobQueueRecord(sql, "1", "default");
                using (new SqlServerTimeoutJob(storage, id, "1", "default"))
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));

                    var record = sql.Query("select * from HangFire.JobQueue").Single();

                    var now = DateTime.UtcNow;
                    Assert.True(now.AddSeconds(-5) < record.FetchedAt);
                }
            });
        }

        [Fact, CleanDatabase]
        public void Dispose_SetsFetchedAtValueToNull_IfThereWereNoCallsToComplete()
        {
            UseConnection((sql, storage) =>
            {
                // Arrange
                var id = CreateJobQueueRecord(sql, "1", "default");
                using (var processingJob = new SqlServerTimeoutJob(storage, id, "1", "default"))
                {
                    // Act
                    processingJob.Dispose();

                    // Assert
                    var record = sql.Query("select * from HangFire.JobQueue").Single();
                    Assert.Null(record.FetchedAt);
                }
            });
        }

        private static int CreateJobQueueRecord(IDbConnection connection, string jobId, string queue)
        {
            const string arrangeSql = @"
insert into HangFire.JobQueue (JobId, Queue, FetchedAt)
values (@id, @queue, getutcdate());
select scope_identity() as Id";

            return (int)connection.Query(arrangeSql, new { id = jobId, queue }).Single().Id;
        }

        private static void UseConnection(Action<IDbConnection, SqlServerStorage> action)
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                var storage = new SqlServerStorage(
                    connection,
                    new SqlServerStorageOptions { SlidingInvisibilityTimeout = TimeSpan.FromSeconds(10) });

                action(connection, storage);
            }
        }
    }
}