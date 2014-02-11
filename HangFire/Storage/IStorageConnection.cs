using System;
using System.Collections.Generic;

namespace HangFire.Storage
{
    public interface IStorageConnection : IDisposable
    {
        IAtomicWriteTransaction CreateWriteTransaction();

        IDisposable AcquireLock(string resource, TimeSpan timeOut);

        IStoredJobs Jobs { get; }

        void AnnounceServer(string serverId, int workerCount, IEnumerable<string> queues);
        void RemoveServer(string serverId);
        void Heartbeat(string serverId);
    }
}