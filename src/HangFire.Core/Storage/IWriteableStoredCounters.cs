using System;

namespace HangFire.Storage
{
    public interface IWriteableStoredCounters
    {
        void Increment(string key);
        void Increment(string key, TimeSpan expireIn);

        void Decrement(string key);
        void Decrement(string key, TimeSpan expireIn);
    }
}