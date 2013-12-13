using System;
using HangFire.States;

namespace HangFire.Client
{
    public interface IJobClient : IDisposable
    {
        string CreateJob(JobMethod data, string[] arguments, JobState state);
    }
}