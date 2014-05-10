using System;
using Moq;
using ServiceStack.Redis;
using Xunit;

namespace HangFire.Redis.Tests
{
    public class RedisProcessingJobFacts
    {
        private const string JobId = "id";
        private const string Queue = "queue";

        private readonly Mock<IRedisClient> _redis;
        private readonly Mock<IRedisTransaction> _transaction;

        public RedisProcessingJobFacts()
        {
            _redis = new Mock<IRedisClient>();
            _transaction = new Mock<IRedisTransaction>();

            _redis.Setup(x => x.CreateTransaction()).Returns(_transaction.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenRedisIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RedisProcessingJob(null, JobId, Queue));

            Assert.Equal("redis", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RedisProcessingJob(_redis.Object, null, Queue));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RedisProcessingJob(_redis.Object, JobId, null));

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
        public void Dispose_CommitsTheTransaction()
        {
            var processingJob = CreateProcessingJob();

            processingJob.Dispose();

            _transaction.Verify(x => x.Commit());
        }

        private RedisProcessingJob CreateProcessingJob()
        {
            return new RedisProcessingJob(_redis.Object, JobId, Queue);
        }
    }
}
