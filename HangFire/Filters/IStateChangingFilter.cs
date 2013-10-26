using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Filters
{
    public interface IStateChangingFilter : IJobFilter
    {
        JobState OnStateChanging(IRedisClient redis, string jobId, JobState state);
    }
}