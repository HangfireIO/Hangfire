using System;

namespace HangFire.Storage
{
    public interface IStorageConnection : IDisposable
    {
        IAtomicWriteTransaction CreateWriteTransaction();

        IDisposable AcquireLock(string resource, TimeSpan timeOut);

        IStoredJobs Jobs { get; }
    }
}