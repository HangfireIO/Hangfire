using System;
using System.Collections.Generic;

namespace HangFire.Storage
{
    public interface IStorageConnection : IDisposable
    {
        IAtomicWriteTransaction CreateWriteTransaction();

        IDisposable AcquireJobLock(string jobId);

        IStoredJobs Jobs { get; }
        IStoredSets Sets { get; }
        JobStorage Storage { get; }

        void AnnounceServer(string serverId, int workerCount, IEnumerable<string> queues);
        void RemoveServer(string serverId);
        void Heartbeat(string serverId);
        void RemoveTimedOutServers(TimeSpan timeOut);
    }
}