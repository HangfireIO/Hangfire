using System;
using System.Data;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerFetchedJobFacts
    {
        private const string JobId = "id";
        private const string Queue = "queue";

        private readonly Mock<IDbConnection> _connection;
        private readonly Mock<IDbTransaction> _transaction;
        private readonly Mock<SqlServerStorage> _storage;

        public SqlServerFetchedJobFacts()
        {
            _connection = new Mock<IDbConnection>();
            _transaction = new Mock<IDbTransaction>();
            _storage = new Mock<SqlServerStorage>(ConnectionUtils.GetConnectionString());
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerFetchedJob(null, _connection.Object, _transaction.Object, JobId, Queue));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerFetchedJob(_storage.Object, null, _transaction.Object, JobId, Queue));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTransactionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerFetchedJob(_storage.Object, _connection.Object, null, JobId, Queue));

            Assert.Equal("transaction", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerFetchedJob(_storage.Object, _connection.Object, _transaction.Object, null, Queue));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerFetchedJob(_storage.Object, _connection.Object, _transaction.Object, JobId, null));

            Assert.Equal("queue", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var fetchedJob = new SqlServerFetchedJob(_storage.Object, _connection.Object, _transaction.Object, JobId, Queue);

            Assert.Equal(JobId, fetchedJob.JobId);
            Assert.Equal(Queue, fetchedJob.Queue);
        }

        [Fact, CleanDatabase]
        public void RemoveFromQueue_CommitsTheTransaction()
        {
            // Arrange
            var processingJob = CreateFetchedJob("1", "default");

            // Act
            processingJob.RemoveFromQueue();

            // Assert
            _transaction.Verify(x => x.Commit());
        }

        [Fact, CleanDatabase]
        public void Requeue_RollsbackTheTransaction()
        {
            // Arrange
            var processingJob = CreateFetchedJob("1", "default");

            // Act
            processingJob.Requeue();

            // Assert
            _transaction.Verify(x => x.Rollback());
        }

        [Fact, CleanDatabase]
        public void Dispose_DisposesTheTransactionAndConnection()
        {
            var processingJob = CreateFetchedJob("1", "queue");

            // Act
            processingJob.Dispose();

            // Assert
            _transaction.Verify(x => x.Dispose());
            _connection.Verify(x => x.Dispose());
        }

        private SqlServerFetchedJob CreateFetchedJob(string jobId, string queue)
        {
            return new SqlServerFetchedJob(_storage.Object, _connection.Object, _transaction.Object, jobId, queue);
        }
    }
}
