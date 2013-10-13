using System.Threading;

namespace HangFire.Server
{
    internal interface IThreadWrappable
    {
        void Work();
        void Dispose(Thread thread);
    }
}