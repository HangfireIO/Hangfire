using System;

namespace HangFire.Storage
{
    public interface IWriteableStoredValues
    {
        void Increment(string key);
        void Decrement(string key);

        void ExpireIn(string key, TimeSpan expireIn);
    }
}