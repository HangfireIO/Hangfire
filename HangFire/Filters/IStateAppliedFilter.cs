using HangFire.Filters;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire
{
    public interface IStateAppliedFilter : IJobFilter
    {
        void OnStateApplied(IRedisTransaction transaction, JobState state);
        void OnStateUnapplied(IRedisTransaction transaction, string state);
    }
}