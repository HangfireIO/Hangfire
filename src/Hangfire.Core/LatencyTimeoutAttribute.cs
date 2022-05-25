// This file is part of Hangfire. Copyright © 2016 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// Represents a job filter that <i>automatically deletes a background job</i>,
    /// when a certain amount of time elapsed since its creation. Deletion
    /// is taking place when a <see cref="Hangfire.Server.Worker"/> attempts
    /// to move a job to the <see cref="ProcessingState"/> state.
    /// </summary>
    public sealed class LatencyTimeoutAttribute : JobFilterAttribute, IElectStateFilter
    {
        private readonly ILog _logger = LogProvider.For<LatencyTimeoutAttribute>();

        private readonly int _timeoutInSeconds;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LatencyTimeoutAttribute"/>
        /// class with the given timeout value.
        /// </summary>
        /// <param name="timeoutInSeconds">Non-negative timeout value in seconds 
        /// that will be used to determine whether to delete a job.</param>
        /// 
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="timeoutInSeconds"/> has a negative value.
        /// </exception>
        public LatencyTimeoutAttribute(int timeoutInSeconds)
        {
            if (timeoutInSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutInSeconds), "Timeout value must be equal or greater than zero.");
            }

            _timeoutInSeconds = timeoutInSeconds;
            LogLevel = LogLevel.Debug;
        }

        /// <summary>
        /// Gets or sets a level for log message that will be produced, when a
        /// background job was deleted due to exceeded timeout.
        /// </summary>
        public LogLevel LogLevel { get; set; }

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
                    Reason = $"Background job has exceeded latency timeout of {_timeoutInSeconds} second(s)"
                };

                _logger.Log(
                    LogLevel,
                    () => $"Background job '{context.BackgroundJob.Id}' has exceeded latency timeout of {_timeoutInSeconds} second(s) and will be deleted");
            }
        }

    }
}