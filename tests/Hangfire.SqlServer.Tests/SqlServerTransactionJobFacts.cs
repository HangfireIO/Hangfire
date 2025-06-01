using System;
using System.Data.Common;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerTransactionJobFacts
    {
        private const string JobId = "id";
        private const string Queue = "queue";

        private readonly Mock<MyDbConnection> _connection;
        private readonly Mock<MyDbTransaction> _transaction;
        private readonly Mock<SqlServerStorage> _storage;

        public SqlServerTransactionJobFacts()
        {
            _connection = new Mock<MyDbConnection> { CallBase = true };
            _transaction = new Mock<MyDbTransaction> { CallBase = true };
            _storage = new Mock<SqlServerStorage>(ConnectionUtils.GetConnectionString());
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerTransactionJob(null, _connection.Object, _transaction.Object, JobId, Queue));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerTransactionJob(_storage.Object, null, _transaction.Object, JobId, Queue));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTransactionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerTransactionJob(_storage.Object, _connection.Object, null, JobId, Queue));

            Assert.Equal("transaction", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerTransactionJob(_storage.Object, _connection.Object, _transaction.Object, null, Queue));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerTransactionJob(_storage.Object, _connection.Object, _transaction.Object, JobId, null));

            Assert.Equal("queue", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var fetchedJob = new SqlServerTransactionJob(_storage.Object, _connection.Object, _transaction.Object, JobId, Queue);

            Assert.Equal(JobId, fetchedJob.JobId);
            Assert.Equal(Queue, fetchedJob.Queue);
        }

        [Fact]
        public void RemoveFromQueue_CommitsTheTransaction()
        {
            // Arrange
            var processingJob = CreateFetchedJob("1", "default");

            // Act
            processingJob.RemoveFromQueue();

            // Assert
            _transaction.Verify(x => x.Commit());
        }

        [Fact]
        public void Requeue_RollsbackTheTransaction()
        {
            // Arrange
            var processingJob = CreateFetchedJob("1", "default");

            // Act
            processingJob.Requeue();

            // Assert
            _transaction.Verify(x => x.Rollback());
        }

        [Fact]
        public void Dispose_DisposesTheTransactionAndConnection()
        {
            var processingJob = CreateFetchedJob("1", "queue");

            // Act
            processingJob.Dispose();

            // Assert
            Assert.True(_transaction.Object.Disposed);
            Assert.True(_connection.Object.Disposed);
        }

        private SqlServerTransactionJob CreateFetchedJob(string jobId, string queue)
        {
            return new SqlServerTransactionJob(_storage.Object, _connection.Object, _transaction.Object, jobId, queue);
        }

        public abstract class MyDbConnection : DbConnection
        {
            public bool Disposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }

        public abstract class MyDbTransaction : DbTransaction
        {
            public bool Disposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
