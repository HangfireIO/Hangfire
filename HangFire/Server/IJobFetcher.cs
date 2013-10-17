using System;
using System.Threading;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal interface IJobFetcher : IDisposable
    {
        string Queue { get; }
        IRedisClient Redis { get; }

        JobPayload DequeueJob(CancellationToken cancellationToken);
    }
}