using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Filters
{
    /// <summary>
    /// Defines methods that are required for a state changing filter.
    /// </summary>
    public interface IStateChangingFilter
    {
        /// <summary>
        /// Called when the current state of the job is being changed to the
        /// specified candidate <paramref name="state"/>.
        /// This state change could be intercepted and the final state could
        /// be changed through returning the different state in the implementation
        /// of this method.
        /// </summary>
        /// <param name="redis">Redis connection.</param>
        /// <param name="jobId">The job, whose state is changing.</param>
        /// <param name="state">The current state candidate.</param>
        /// <returns>The new state candidate, which will be applied to the job.</returns>
        JobState OnStateChanging(IRedisClient redis, string jobId, JobState state);
    }
}