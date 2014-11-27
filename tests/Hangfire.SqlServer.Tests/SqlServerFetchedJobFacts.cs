using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Hangfire.Sql;
using Moq;
using Xunit;

namespace Hangfire.SqlServer.Tests {
    public class SqlServerFetchedJobFacts {
        private const string JobId = "id";
        private const string Queue = "queue";

        private IConnectionProvider _connectionProvider;

        public SqlServerFetchedJobFacts() {
            _connectionProvider = ConnectionUtils.CreateConnectionProvider();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull() {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlFetchedJob(null, new SqlBook(), 1, JobId, Queue));

            Assert.Equal("connectionProvider", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull() {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlFetchedJob(ConnectionUtils.CreateConnectionProvider(), new SqlBook(), 1, null, Queue));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull() {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlFetchedJob(ConnectionUtils.CreateConnectionProvider(), new SqlBook(), 1, JobId, null));

            Assert.Equal("queue", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties() {
            var fetchedJob = new SqlFetchedJob(ConnectionUtils.CreateConnectionProvider(), new SqlBook(), 1, JobId, Queue);

            Assert.Equal(1, fetchedJob.Id);
            Assert.Equal(JobId, fetchedJob.JobId);
            Assert.Equal(Queue, fetchedJob.Queue);
        }

        [Fact, CleanDatabase("[HangFire].[JobQueue]")]
        public void RemoveFromQueue_ReallyDeletesTheJobFromTheQueue() {
            // Arrange
            int id = 0;
            UseConnection((sql, tx) => {
                id = CreateJobQueueRecord(sql, tx, "1", "default");
            });
            var processingJob = new SqlFetchedJob(ConnectionUtils.CreateConnectionProvider(), new SqlBook(), id, "1", "default");

            // Act
            processingJob.RemoveFromQueue();

            // Assert
            UseConnection((sql, tx) => {
                var count = sql.Query<int>("select count(*) from HangFire.JobQueue", transaction:tx).Single();
                Assert.Equal(0, count);
            });
        }

        [Fact, CleanDatabase]
        public void RemoveFromQueue_DoesNotDelete_UnrelatedJobs() {
            // Arrange
            UseConnection((sql, tx) => {
                CreateJobQueueRecord(sql, tx, "1", "default");
                CreateJobQueueRecord(sql, tx, "1", "critical");
                CreateJobQueueRecord(sql, tx, "2", "default");
            });
            var fetchedJob = new SqlFetchedJob(_connectionProvider, new SqlBook(), 999, "1", "default");

            // Act
            fetchedJob.RemoveFromQueue();

            // Assert
            UseConnection((sql, tx) => {
                var count = sql.Query<int>("select count(*) from HangFire.JobQueue", transaction: tx).Single();
                Assert.Equal(3, count);
            });
        }

        [Fact, CleanDatabase("[HangFire].[JobQueue]")]
        public void Requeue_SetsFetchedAtValueToNull() {
            SqlFetchedJob processingJob = null;
            UseConnection((sql, tx) => {
                // Arrange
                var id = CreateJobQueueRecord(sql, tx, "1", "default");
                processingJob = new SqlFetchedJob(ConnectionUtils.CreateConnectionProvider(), new SqlBook(), id, "1", "default");
            });

            // Act
            processingJob.Requeue();

            // Assert
            UseConnection((sql, tx) => {
                var record = sql.Query("select * from HangFire.JobQueue", transaction: tx).Single();
                Assert.Null(record.FetchedAt);
            });
        }

        [Fact, CleanDatabase("[HangFire].[JobQueue]")]
        public void Dispose_SetsFetchedAtValueToNull_IfThereWereNoCallsToComplete() {
            // Arrange
            SqlFetchedJob processingJob = null;
            UseConnection((sql, tx) => {
                var id = CreateJobQueueRecord(sql, tx, "1", "default");
                processingJob = new SqlFetchedJob(ConnectionUtils.CreateConnectionProvider(), new SqlBook(), id, "1", "default");
            });

            // Act
            processingJob.Dispose();

            // Assert
            UseConnection((sql, tx) => {
                var record = sql.Query("select * from HangFire.JobQueue", transaction: tx).Single();
                Assert.Null(record.FetchedAt);
            });
        }

        private static int CreateJobQueueRecord(IDbConnection connection, IDbTransaction transaction, string jobId, string queue) {
            const string arrangeSql = @"
insert into HangFire.JobQueue (JobId, Queue, FetchedAt)
values (@id, @queue, getutcdate());
select scope_identity() as Id";

            return (int)connection.Query(arrangeSql, new { id = jobId, queue = queue }, transaction: transaction).Single().Id;
        }

        private void UseConnection(Action<IDbConnection, IDbTransaction> action) {
            using (var connection = _connectionProvider.CreateAndOpenConnection()) {
                using (var transaction = connection.BeginTransaction()) {
                    action(connection, transaction);
                    transaction.Commit();
                }
                connection.Close();
            }
        }
    }
}
