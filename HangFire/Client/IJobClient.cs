using System;
using HangFire.States;

namespace HangFire.Client
{
    public interface IJobClient : IDisposable
    {
        string CreateJob(JobInvocationData data, string[] arguments, JobState state);
    }
}