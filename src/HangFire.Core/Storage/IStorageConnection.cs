using System;
using System.Collections.Generic;
using HangFire.Server;

namespace HangFire.Storage
{
    public interface IStorageConnection : IDisposable
    {
        JobStorage Storage { get; }

        IWriteOnlyTransaction CreateWriteTransaction();
        IJobFetcher CreateFetcher(IEnumerable<string> queueNames);

        string CreateExpiredJob(
            InvocationData invocationData,
            string[] arguments,
            IDictionary<string, string> parameters,
            TimeSpan expireIn);

        void SetJobParameter(string id, string name, string value);
        string GetJobParameter(string id, string name);

        IDisposable AcquireJobLock(string jobId);
        StateAndInvocationData GetJobStateAndInvocationData(string id);
        void DeleteJobFromQueue(string jobId, string queue);

        string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore);

        void AnnounceServer(string serverId, int workerCount, IEnumerable<string> queues);
        void RemoveServer(string serverId);
        void Heartbeat(string serverId);
        int RemoveTimedOutServers(TimeSpan timeOut);
    }
}