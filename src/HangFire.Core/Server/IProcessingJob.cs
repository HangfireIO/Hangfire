using System;
using HangFire.Server.Performing;

namespace HangFire.Server
{
    public interface IProcessingJob : IDisposable
    {
        string JobId { get; }
        string Queue { get; }

        void Process(WorkerContext context, IJobPerformanceProcess process);
    }
}