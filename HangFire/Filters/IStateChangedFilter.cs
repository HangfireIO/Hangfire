using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Filters
{
    public interface IStateChangedFilter : IJobFilter
    {
        void OnStateApplied(IRedisTransaction transaction, string jobId, JobState state);
        void OnStateUnapplied(IRedisTransaction transaction, string jobId, string state);
    }
}