using System;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Redis.Tests
{
    public class RedisProcessingJobFacts
    {
        private const string JobId = "id";
        private const string Queue = "queue";

        private readonly Mock<IStorageConnection> _connection;

        public RedisProcessingJobFacts()
        {
            _connection = new Mock<IStorageConnection>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RedisProcessingJob(null, JobId, Queue));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RedisProcessingJob(_connection.Object, null, Queue));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RedisProcessingJob(_connection.Object, JobId, null));

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

        private RedisProcessingJob CreateProcessingJob()
        {
            return new RedisProcessingJob(_connection.Object, JobId, Queue);
        }
    }
}
