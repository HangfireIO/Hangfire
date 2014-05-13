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
                () => new SqlServerFetchedJob(null, JobId, Queue));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerFetchedJob(_connection.Object, null, Queue));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerFetchedJob(_connection.Object, JobId, null));

            Assert.Equal("queue", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var processingJob = CreateProcessingJob();

            Assert.Equal(JobId, processingJob.JobId);
            Assert.Equal(Queue, processingJob.Queue);
        }

        [Fact, CleanDatabase]
        public void DeleteJobFromQueue_ReallyDeletesTheJobFromTheQueue()
        {
            const string arrangeSql = @"
insert into HangFire.JobQueue (JobId, Queue)
values (@id, @queue)";

            UseConnection(sql =>
            {
                // Arrange
                sql.Execute(arrangeSql, new { id = "1", queue = "default" });
                var processingJob = new SqlServerFetchedJob(sql, "1", "default");

                // Act
                processingJob.Dispose();

                // Assert
                var count = sql.Query<int>("select count(*) from HangFire.JobQueue").Single();
                Assert.Equal(0, count);
            });
        }

        [Fact, CleanDatabase]
        public void DeleteJobFromQueue_DoesNotDelete_UnrelatedJobs()
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
                        new { id = "1", queue = "critical" },
                        new { id = "2", queue = "default" } 
                    });

                var processingJob = new SqlServerFetchedJob(sql, "1", "default");

                // Act
                processingJob.Dispose();

                // Assert
                var count = sql.Query<int>("select count(*) from HangFire.JobQueue").Single();
                Assert.Equal(2, count);
            });
        }

        private SqlServerFetchedJob CreateProcessingJob()
        {
            return new SqlServerFetchedJob(_connection.Object, JobId, Queue);
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
