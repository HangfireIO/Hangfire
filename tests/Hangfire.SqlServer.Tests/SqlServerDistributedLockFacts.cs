using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using Dapper;
using Xunit;

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

        [Fact, CleanDatabase]
        public void Ctor_ThrowsAnException_WhenResourceIsNullOrEmpty()
        {
            UseConnection(connection =>
            {
                var storage = CreateStorage(connection);

                var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerDistributedLock(storage, "", _timeout));

                Assert.Equal("resource", exception.ParamName);
            });
        }

        [Fact, CleanDatabase]
        public void Ctor_AcquiresExclusiveApplicationLock_OnSession()
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
            });
        }

        [Fact, CleanDatabase]
        public void Ctor_ThrowsAnException_IfLockCanNotBeGranted()
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
                }));
            thread.Start();

            lockAcquired.Wait();

            UseConnection(connection2 =>
            {
                var storage = CreateStorage(connection2);
                Assert.Throws<SqlServerDistributedLockException>(
                    () =>
                    {
                        using (new SqlServerDistributedLock(storage, "exclusive", _timeout))
                        {
                        }
                    });
            });

            releaseLock.Set();
            thread.Join();
        }

        [Fact, CleanDatabase]
        public void Dispose_ReleasesExclusiveApplicationLock()
        {
            UseConnection(sql =>
            {
                var storage = CreateStorage(sql);
                var distributedLock = new SqlServerDistributedLock(storage, "hello", _timeout);
                distributedLock.Dispose();

                var lockMode = sql.Query<string>(
                    "select applock_mode('public', 'hello', 'session')").Single();

                Assert.Equal("NoLock", lockMode);
            });
        }

        private SqlServerStorage CreateStorage(SqlConnection connection)
        {
            return new SqlServerStorage(connection);
        }

        private void UseConnection(Action<SqlConnection> action)
        {
            using (var connection = ConnectionUtils.CreateConnection())
            {
                action(connection);
            }
        }
    }
}
