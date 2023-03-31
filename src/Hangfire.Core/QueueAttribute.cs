// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Linq;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// Represents attribute, that is used to determine queue name
    /// for background jobs. It can be applied to the methods and classes. 
    /// If the attribute is not applied neither to the method, nor the class, 
    /// then default queue will be used.
    /// </summary>
    /// 
    /// <example><![CDATA[
    /// 
    /// [Queue("high")]
    /// public class ErrorService
    /// {
    ///     public void ReportError(string message) { }
    /// 
    ///     [Queue("critical")]
    ///     public void ReportFatal(string message) { }
    /// }
    /// 
    /// // Background job will be placed on the 'high' queue.
    /// BackgroundJob.Enqueue<ErrorService>(x => x.ReportError("Something bad happened"));
    /// 
    /// // Background job will be placed on the 'critical' queue.
    /// BackgroundJob.Enqueue<ErrorService>(x => x.ReportFatal("Really bad thing!"));
    /// 
    /// ]]></example>
    public sealed class QueueAttribute : JobFilterAttribute, IElectStateFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueAttribute"/> class
        /// using the specified queue name.
        /// </summary>
        /// <param name="queue">Queue name.</param>
        public QueueAttribute(string queue)
        {
            Queue = queue;
            Order = Int32.MaxValue;
        }

        /// <summary>
        /// Gets the queue name that will be used for background jobs.
        /// </summary>
        public string Queue { get; }

        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is EnqueuedState enqueuedState)
            {
                enqueuedState.Queue = String.Format(Queue, context.BackgroundJob.Job.Args.ToArray());
            }
        }
    }
}
