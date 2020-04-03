using System;

namespace Hangfire
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public class SystemClock : IClock
    {
        private static readonly Lazy<IClock> Cached = new Lazy<IClock>(() => new SystemClock());
        private static readonly object LockObject = new object();
        private static IClock _current;

        public static IClock Current
        {
            get
            {
                lock (LockObject)
                {
                    return _current ?? Cached.Value;
                }
            }
            set
            {
                lock (LockObject)
                {
                    _current = value;
                }
            }
        }

        public DateTime UtcNow => DateTime.UtcNow;
    }
}
