using HangFire.Filters;
using HangFire.States;
using ServiceStack.Redis;

namespace HangFire
{
    public interface IStateAppliedFilter : IJobFilter
    {
        void OnStateApplied(IRedisTransaction transaction, string jobId, JobState state);
        void OnStateUnapplied(IRedisTransaction transaction, string jobId, string state);
    }
}