using System;

namespace HangFire.Storage
{
    public interface IWriteOnlyTransaction : IDisposable
    {
        IWriteOnlyPersistentValue Values { get; }
        IWriteOnlyPersistentSet Sets { get; }
        IWriteOnlyPersistentList Lists { get; }
        IWriteOnlyPersistentQueue Queues { get; }
        IWriteOnlyPersistentJob Jobs { get; }
        IWriteOnlyPersistentCounter Counters { get; }

        bool Commit();
    }
}