using System;
using HangFire.States;

namespace HangFire.Client
{
    public interface IJobClient : IDisposable
    {
        string CreateJob(JobMethod method, string[] arguments, JobState state);
    }
}