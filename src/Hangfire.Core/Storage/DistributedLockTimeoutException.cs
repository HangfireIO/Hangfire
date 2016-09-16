using System;

namespace Hangfire.Storage
{
    public class DistributedLockTimeoutException : TimeoutException
    {
        public DistributedLockTimeoutException(string resource)
            : base(
                $"Timeout expired. The timeout elapsed prior to obtaining a distributed lock on the '{resource}' resource."
                )
        {
        }
    }
}
