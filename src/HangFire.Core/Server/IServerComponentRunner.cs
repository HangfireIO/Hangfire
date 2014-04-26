using System;

namespace HangFire.Server
{
    public interface IServerComponentRunner : IDisposable
    {
        void Start();
        void Stop();
    }
}