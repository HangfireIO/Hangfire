using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using Dapper;
using Moq;
using Xunit;

namespace HangFire.SqlServer.Tests
{
    public class SqlServerDistributedLockFacts
    {
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

        [Fact]
        public void Ctor_ThrowsAnException_WhenResourceIsNullOrEmpty()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerDistributedLock("", _timeout, new Mock<IDbConnection>().Object));

            Assert.Equal("resource", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlServerDistributedLock("hello", _timeout, null));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact, CleanDatabase]
        public void Ctor_AcquiresExclusiveApplicationLock_OnSession()
        {
            UseConnection(sql =>
            {
                // ReSharper disable once UnusedVariable
                var distributedLock = new SqlServerDistributedLock("hello", _timeout, sql);

                var lockMode = sql.Query<string>(
                    "select applock_mode('public', 'hello', 'session')").Single();

                Assert.Equal("Exclusive", lockMode);
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
                    using (new SqlServerDistributedLock("exclusive", _timeout, connection1))
                    {
                        lockAcquired.Set();
                        releaseLock.Wait();
                    }
                }));
            thread.Start();

            lockAcquired.Wait();

            UseConnection(connection2 => 
                Assert.Throws<SqlServerDistributedLockException>(
                    () => new SqlServerDistributedLock("exclusive", _timeout, connection2)));

            releaseLock.Set();
            thread.Join();
        }

        [Fact, CleanDatabase]
        public void Dispose_ReleasesExclusiveApplicationLock()
        {
            UseConnection(sql =>
            {
                var distributedLock = new SqlServerDistributedLock("hello", _timeout, sql);
                distributedLock.Dispose();

                var lockMode = sql.Query<string>(
                    "select applock_mode('public', 'hello', 'session')").Single();

                Assert.Equal("NoLock", lockMode);
            });
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
