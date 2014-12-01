using Hangfire.Storage;
using System;
using System.Data;

namespace Hangfire.Sql {
    public interface IDistributedLockAcquirer {
        IDistributedLock AcquireLock(string resource, TimeSpan timeout, IDbConnection connection);
    }
}