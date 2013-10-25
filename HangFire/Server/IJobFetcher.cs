using System;
using System.Threading;

namespace HangFire.Server
{
    internal interface IJobFetcher : IDisposable
    {
        JobPayload DequeueJob(CancellationToken cancellationToken);
        void Stop();
    }
}