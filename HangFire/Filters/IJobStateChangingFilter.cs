using HangFire.Filters;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire
{
    public interface IJobStateChangingFilter : IJobFilter
    {
        JobState OnStateChanged(IRedisClient redis, JobState state);
    }
}