using System;

namespace HangFire.Tests
{
    public class TestJob : BackgroundJob, IDisposable
    {
        public static bool Performed;
        public static bool Disposed;

        public override void Perform()
        {
            Performed = true;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
