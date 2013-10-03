using HangFire.Filters;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire
{
    public interface IJobStateFilter : IJobFilter
    {
        JobState OnJobState(IRedisClient redis, JobState state);
    }
}