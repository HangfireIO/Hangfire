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
        private readonly RedisConnection _redisConnection;

        public RedisProcessingJobFacts()
        {
            _redis = new Mock<IRedisClient>();
            _transaction = new Mock<IRedisTransaction>();

            _redis.Setup(x => x.CreateTransaction()).Returns(_transaction.Object);

            _redisConnection = new RedisConnection(_redis.Object);
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
                () => new RedisProcessingJob(_redisConnection, null, Queue));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RedisProcessingJob(_redisConnection, JobId, null));

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
        public void Dispose_CommitsTransaction()
        {
            var processingJob = CreateProcessingJob();

            processingJob.Dispose();

            _transaction.Verify(x => x.Commit());
        }

        private RedisProcessingJob CreateProcessingJob()
        {
            return new RedisProcessingJob(_redisConnection, JobId, Queue);
        }
    }
}
