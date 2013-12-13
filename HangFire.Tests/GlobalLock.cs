using System.Threading;

namespace HangFire.Tests
{
    public static class GlobalLock
    {
        private static readonly object Lock = new object();

        public static void Acquire()
        {
            Monitor.Enter(Lock);
        }

        public static void Release()
        {
            Monitor.Exit(Lock);
        }
    }
}
