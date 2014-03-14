using System;
using System.Collections.Generic;

namespace HangFire.Storage
{
    public interface IWriteOnlyPersistentJob
    {
        void Expire(string jobId, TimeSpan expireIn);
        void Persist(string jobId);

        void SetState(string jobId, string state, Dictionary<string, string> stateProperties);

        void AppendHistory(string jobId, Dictionary<string, string> properties);
    }
}