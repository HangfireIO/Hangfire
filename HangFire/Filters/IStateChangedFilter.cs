using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Filters
{
    /// <summary>
    /// Provides methods that are required for a state changed filter.
    /// </summary>
    public interface IStateChangedFilter
    {
        /// <summary>
        /// Called after the specified <paramref name="state"/> was applied
        /// to the job within the given <paramref name="transaction"/>.
        /// </summary>
        /// <param name="transaction">The current transaction.</param>
        /// <param name="jobId">The job, whose state was changed.</param>
        /// <param name="state">The applied state.</param>
        void OnStateApplied(IRedisTransaction transaction, string jobId, JobState state);

        /// <summary>
        /// Called when the state with specified <paramref name="stateName"/> was 
        /// unapplied from the job within the given <paramref name="transaction"/>.
        /// </summary>
        /// <param name="transaction">The current transaction.</param>
        /// <param name="jobId">The job, whose state is changing.</param>
        /// <param name="stateName">The unapplied state name.</param>
        void OnStateUnapplied(IRedisTransaction transaction, string jobId, string stateName);
    }
}