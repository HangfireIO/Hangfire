using System;
using System.Data;
using Hangfire.Sql;
using Hangfire.Storage;

namespace Hangfire.Oracle {
    public class OracleDistributedLockAcquirer : IDistributedLockAcquirer {
        public IDistributedLock AcquireLock(string resource, TimeSpan timeout, IDbConnection connection) {
            return new OracleDistributedLock();
        }
    }
}