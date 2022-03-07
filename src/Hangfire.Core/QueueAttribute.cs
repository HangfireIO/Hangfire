// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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
