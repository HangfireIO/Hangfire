using System;
using Moq;
using ServiceStack.Redis;
using Xunit;

namespace HangFire.Redis.Tests
{
    public class RedisFetchedJobFacts
    {
        private const string JobId = "id";
        private const string Queue = "queue";

        private readonly Mock<IRedisClient> _redis;
        private readonly Mock<IRedisTransaction> _transaction;
        private readonly RedisConnection _redisConnection;

        public RedisFetchedJobFacts()
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
                () => new RedisFetchedJob(null, JobId, Queue));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RedisFetchedJob(_redisConnection, null, Queue));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new RedisFetchedJob(_redisConnection, JobId, null));

            Assert.Equal("queue", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var fetchedJob = CreateFetchedJob();

            Assert.Equal(JobId, fetchedJob.JobId);
            Assert.Equal(Queue, fetchedJob.Queue);
        }

        [Fact]
        public void RemoveFromQueue_CommitsTransaction()
        {
            var fetchedJob = CreateFetchedJob();

            fetchedJob.RemoveFromQueue();

            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void Dispose_DoesNotCommitTransaction()
        {
            var fetchedJob = CreateFetchedJob();

            fetchedJob.Dispose();

            _transaction.Verify(x => x.Commit(), Times.Never);
        }

        private RedisFetchedJob CreateFetchedJob()
        {
            return new RedisFetchedJob(_redisConnection, JobId, Queue);
        }
    }
}
