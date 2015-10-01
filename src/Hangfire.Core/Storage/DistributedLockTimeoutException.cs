using System;

namespace Hangfire.Storage
{
    public class DistributedLockTimeoutException : TimeoutException
    {
        public DistributedLockTimeoutException(string resource)
            : base(String.Format("Timeout expired. The timeout elapsed prior to obtaining a distributed lock on the '{0}' resource.", resource))
        {
        }
    }
}
