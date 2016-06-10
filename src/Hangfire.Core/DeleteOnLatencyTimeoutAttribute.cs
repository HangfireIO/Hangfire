using System;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// Represents a job filter that <i>automatically deletes a background job</i>,
    /// when a certain amount of time elapsed since its creation. Deletion
    /// is taking place when a <see cref="Hangfire.Server.Worker"/> tries
    /// to move a job to the <see cref="ProcessingState"/> state.
    /// </summary>
    public sealed class DeleteOnLatencyTimeoutAttribute : JobFilterAttribute, IElectStateFilter
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        private readonly int _timeoutInSeconds;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteOnLatencyTimeoutAttribute"/>
        /// class with the given timeout value.
        /// </summary>
        /// <param name="timeoutInSeconds">Non-negative timeout value in seconds 
        /// that will be used to determine whether to delete a job.</param>
        /// 
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="timeoutInSeconds"/> has a negative value.
        /// </exception>
        public DeleteOnLatencyTimeoutAttribute(int timeoutInSeconds)
        {
            if (timeoutInSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutInSeconds), "Timeout value must be equal or greater than zero.");
            }

            _timeoutInSeconds = timeoutInSeconds;
            LogEvents = true;
        }

        /// <summary>
        /// Gets or sets whether to produce a warning message, when a background 
        /// job was deleted after the specified timeout.
        /// </summary>
        public bool LogEvents { get; set; }

        /// <inheritdoc />
        public void OnStateElection(ElectStateContext context)
        {
            var state = context.CandidateState as ProcessingState;
            if (state == null)
            {
                //this filter only accepts Processing
                return;
            }

            var elapsedTime = DateTime.UtcNow - context.BackgroundJob.CreatedAt;

            if (elapsedTime.TotalSeconds > _timeoutInSeconds)
            {
                context.CandidateState = new DeletedState
                {
                    Reason = $"Job has exceeded latency timeout of {_timeoutInSeconds} seconds"
                };

                if (LogEvents)
                {
                    Logger.Warn(
                        $"Failed to process the job '{context.BackgroundJob.Id}': Job has exceeded latency timeout of {_timeoutInSeconds} seconds");
                }
            }
        }

    }
}