using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Common.States;

namespace HangFire.Storage
{
    public interface IWriteOnlyTransaction : IDisposable
    {
        // Job operations
        void ExpireJob(string jobId, TimeSpan expireIn);
        void PersistJob(string jobId);
        void SetJobState(string jobId, State state);
        void AddJobState(string jobId, State state);

        // Queue operations
        void AddToQueue(string queue, string jobId);

        // Counter operations
        void IncrementCounter(string key);
        void IncrementCounter(string key, TimeSpan expireIn);
        void DecrementCounter(string key);
        void DecrementCounter(string key, TimeSpan expireIn);

        // Set operations
        void AddToSet(string key, string value);
        void AddToSet(string key, string value, double score);
        void RemoveFromSet(string key, string value);

        // List operations
        void InsertToList(string key, string value);
        void RemoveFromList(string key, string value);
        void TrimList(string key, int keepStartingFrom, int keepEndingAt);

        // Value operations
        void IncrementValue(string key);
        void DecrementValue(string key);
        void ExpireValue(string key, TimeSpan expireIn);

        bool Commit();
    }
}