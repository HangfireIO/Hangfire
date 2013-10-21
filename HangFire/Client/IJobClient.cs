using System;
using HangFire.States;

namespace HangFire.Client
{
    internal interface IJobClient : IDisposable
    {
        string CreateJob(
            string jobId, Type jobType, JobState state, object args);
    }
}