using System;
using System.Collections.Generic;
using HangFire.Server;

namespace HangFire.Storage
{
    public interface IStorageConnection : IDisposable
    {
        IAtomicWriteTransaction CreateWriteTransaction();
        IJobFetcher CreateFetcher(IEnumerable<string> queueNames);

        IDisposable AcquireJobLock(string jobId);

        IStoredJobs Jobs { get; }
        IStoredSets Sets { get; }
        JobStorage Storage { get; }

        string CreateExpiredJob(
            InvocationData invocationData,
            string[] arguments,
            IDictionary<string, string> parameters,
            TimeSpan expireIn);

        void AnnounceServer(string serverId, int workerCount, IEnumerable<string> queues);
        void RemoveServer(string serverId);
        void Heartbeat(string serverId);
        int RemoveTimedOutServers(TimeSpan timeOut);
    }
}