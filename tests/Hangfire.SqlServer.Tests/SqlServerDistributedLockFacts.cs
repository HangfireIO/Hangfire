extern alias ReferencedDapper;

using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Transactions;
using ReferencedDapper::Dapper;
using Hangfire.Storage;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerDistributedLockFacts
    {
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerDistributedLock(null, "hello", _timeout));

            Assert.Equal("storage", exception.ParamName);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Ctor_ThrowsAnException_WhenResourceIsNullOrEmpty(bool useMicrosoftDataSqlClient)
        {
            UseConnection(connection =>
            {
                var storage = CreateStorage(connection);

                var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerDistributedLock(storage, "", _timeout));

                Assert.Equal("resource", exception.ParamName);
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Ctor_AcquiresExclusiveApplicationLock_OnSession(bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                // ReSharper disable once UnusedVariable
                var storage = CreateStorage(sql);
                using (new SqlServerDistributedLock(storage, "hello", _timeout))
                {
                    var lockMode = sql.Query<string>(
                        "select applock_mode('public', 'hello', 'session')").Single();

                    Assert.Equal("Exclusive", lockMode);
                }
            }, useMicrosoftDataSqlClient);
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Ctor_ThrowsAnException_IfLockCanNotBeGranted(bool useMicrosoftDataSqlClient)
        {
            var releaseLock = new ManualResetEventSlim(false);
            var lockAcquired = new ManualResetEventSlim(false);

            var thread = new Thread(
                () => UseConnection(connection1 =>
                {
                    var storage = CreateStorage(connection1);
                    using (new SqlServerDistributedLock(storage, "exclusive", _timeout))
                    {
                        lockAcquired.Set();
                        releaseLock.Wait();
                    }
                }, useMicrosoftDataSqlClient));
            thread.Start();

            lockAcquired.Wait();

            UseConnection(connection2 =>
            {
                var storage = CreateStorage(connection2);
                Assert.Throws<DistributedLockTimeoutException>(
                    () =>
                    {
                        using (new SqlServerDistributedLock(storage, "exclusive", _timeout))
                        {
                        }
                    });
            }, useMicrosoftDataSqlClient);

            releaseLock.Set();
            thread.Join();
        }

        [Theory, CleanDatabase]
        [InlineData(false), InlineData(true)]
        public void Dispose_ReleasesExclusiveApplicationLock(bool useMicrosoftDataSqlClient)
        {
            UseConnection(sql =>
            {
                var storage = CreateStorage(sql);
                var distributedLock = new SqlServerDistributedLock(storage, "hello", _timeout);
                distributedLock.Dispose();

                var lockMode = sql.Query<string>(
                    "select applock_mode('public', 'hello', 'session')").Single();

                Assert.Equal("NoLock", lockMode);
            }, useMicrosoftDataSqlClient);
        }

        [Fact, CleanDatabase(IsolationLevel.Unspecified)]
        public void DistributedLocks_AreReEntrant_FromTheSameThread_OnTheSameResource()
        {
            var storage = new SqlServerStorage(ConnectionUtils.GetConnectionString());
            
            using (new SqlServerDistributedLock(storage, "hello", TimeSpan.FromMinutes(5)))
            using (new SqlServerDistributedLock(storage, "hello", TimeSpan.FromMinutes(5)))
            {
                Assert.True(true);
            }
        }

        [Fact, CleanDatabase(IsolationLevel.Unspecified)]
        public void InnerDistributedLock_DoesNotConsumeADatabaseConnection()
        {
            // Arrange
            var storage = new SqlServerStorage(ConnectionUtils.GetConnectionString());

            // Act
            using (var outer = new SqlServerDistributedLock(storage, "hello", TimeSpan.FromMinutes(5)))
            using (var inner = new SqlServerDistributedLock(storage, "hello", TimeSpan.FromMinutes(5)))
            {
                // Assert
                var field = typeof(SqlServerDistributedLock).GetField("_connection",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(field);

                Assert.NotNull(field.GetValue(outer));
                Assert.Null(field.GetValue(inner));
            }
        }

        private static SqlServerStorage CreateStorage(DbConnection connection)
        {
            return new SqlServerStorage(connection);
        }

        private static void UseConnection(Action<DbConnection> action, bool useMicrosoftDataSqlClient)
        {
            using (var connection = ConnectionUtils.CreateConnection(useMicrosoftDataSqlClient))
            {
                action(connection);
            }
        }
    }
}
