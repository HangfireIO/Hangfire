using HangFire.Filters;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire
{
    public interface IStateChangedFilter : IJobFilter
    {
        JobState OnStateChanged(IRedisClient redis, JobState state);
    }
}