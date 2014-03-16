using System;

namespace HangFire.Server
{
    public class JobPerformanceException : Exception
    {
        public JobPerformanceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
