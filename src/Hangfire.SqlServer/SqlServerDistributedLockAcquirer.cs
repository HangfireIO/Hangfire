using System;
using System.Data;
using Hangfire.Sql;
using Hangfire.Storage;

namespace Hangfire.SqlServer {
    public class SqlServerDistributedLockAcquirer : IDistributedLockAcquirer {
        public IDistributedLock AcquireLock(string resource, TimeSpan timeout, IDbConnection connection) {
            return new SqlServerDistributedLock(
                String.Format("HangFire:{0}", resource),
                timeout,
                connection);
        }
    }
}