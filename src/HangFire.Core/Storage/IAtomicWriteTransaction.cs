using System;

namespace HangFire.Storage
{
    public interface IAtomicWriteTransaction : IDisposable
    {
        IWriteableStoredValues Values { get; }
        IWriteableStoredSets Sets { get; }
        IWriteableStoredLists Lists { get; }
        IWriteableJobQueue Queues { get; }
        IWriteableStoredJobs Jobs { get; }
        IWriteableStoredCounters Counters { get; }

        bool Commit();
    }
}