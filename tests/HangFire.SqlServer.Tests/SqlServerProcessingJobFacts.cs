using System;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.SqlServer.Tests
{
    public class SqlServerProcessingJobFacts
    {
        private const string JobId = "id";
        private const string Queue = "queue";

        private readonly Mock<IStorageConnection> _connection;

        public SqlServerProcessingJobFacts()
        {
            _connection = new Mock<IStorageConnection>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerProcessingJob(null, JobId, Queue));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerProcessingJob(_connection.Object, null, Queue));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerProcessingJob(_connection.Object, JobId, null));

            Assert.Equal("queue", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var processingJob = CreateProcessingJob();

            Assert.Equal(JobId, processingJob.JobId);
            Assert.Equal(Queue, processingJob.Queue);
        }

        [Fact]
        public void Dispose_CallsDeleteFromQueue()
        {
            var processingJob = CreateProcessingJob();

            processingJob.Dispose();

            _connection.Verify(x => x.DeleteJobFromQueue(JobId, Queue));
        }

        private SqlServerProcessingJob CreateProcessingJob()
        {
            return new SqlServerProcessingJob(_connection.Object, JobId, Queue);
        }
    }
}
