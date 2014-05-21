using System;
using System.Data;
using System.Linq;
using Dapper;
using Moq;
using Xunit;

namespace HangFire.SqlServer.Tests
{
    public class SqlServerFetchedJobFacts
    {
        private const string JobId = "id";
        private const string Queue = "queue";

        private readonly Mock<IDbConnection> _connection;

        public SqlServerFetchedJobFacts()
        {
            _connection = new Mock<IDbConnection>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerFetchedJob(null, 1, JobId, Queue));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerFetchedJob(_connection.Object, 1, null, Queue));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerFetchedJob(_connection.Object, 1, JobId, null));

            Assert.Equal("queue", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var fetchedJob = CreateFetchedJob();

            Assert.Equal(JobId, fetchedJob.JobId);
            Assert.Equal(Queue, fetchedJob.Queue);
        }

        [Fact, CleanDatabase]
        public void RemoveFromQueue_ReallyDeletesTheJobFromTheQueue()
        {
            const string arrangeSql = @"
insert into HangFire.JobQueue (JobId, Queue)
values (@id, @queue);
select scope_identity() as Id;";

            UseConnection(sql =>
            {
                // Arrange
                var id = (int)sql.Query(arrangeSql, new { id = "1", queue = "default" }).Single().Id;
                var processingJob = new SqlServerFetchedJob(sql, id, "1", "default");

                // Act
                processingJob.RemoveFromQueue();

                // Assert
                var count = sql.Query<int>("select count(*) from HangFire.JobQueue").Single();
                Assert.Equal(0, count);
            });
        }

        [Fact, CleanDatabase]
        public void RemoveFromQueue_DoesNotDelete_UnrelatedJobs()
        {
            const string arrangeSql = @"
insert into HangFire.JobQueue (JobId, Queue)
values (@id, @queue)";

            UseConnection(sql =>
            {
                // Arrange
                sql.Execute(
                    arrangeSql,
                    new[]
                    { 
                        new { id = "1", queue = "default" },
                        new { id = "1", queue = "critical" },
                        new { id = "2", queue = "default" } 
                    });

                var fetchedJob = new SqlServerFetchedJob(sql, 999, "1", "default");

                // Act
                fetchedJob.RemoveFromQueue();

                // Assert
                var count = sql.Query<int>("select count(*) from HangFire.JobQueue").Single();
                Assert.Equal(3, count);
            });
        }

        [Fact, CleanDatabase]
        public void Dispose_DoesNotRemoveTheJobFromTheQueue()
        {
            const string arrangeSql = @"
insert into HangFire.JobQueue (JobId, Queue)
values (@id, @queue);
select scope_identity() as Id";

            UseConnection(sql =>
            {
                // Arrange
                var id = (int)sql.Query(arrangeSql, new { id = "1", queue = "default" }).Single().Id;
                var processingJob = new SqlServerFetchedJob(sql, id, "1", "default");

                // Act
                processingJob.Dispose();

                // Assert
                var count = sql.Query<int>("select count(*) from HangFire.JobQueue").Single();
                Assert.Equal(1, count);
            });
        }

        private SqlServerFetchedJob CreateFetchedJob()
        {
            return new SqlServerFetchedJob(_connection.Object, 1, JobId, Queue);
        }

        private static void UseConnection(Action<IDbConnection> action)
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                action(connection);
            }
        }
    }
}
