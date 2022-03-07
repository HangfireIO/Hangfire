// This file is part of Hangfire. Copyright � 2013-2014 Sergey Odinokov.
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

using Hangfire.Annotations;
using Hangfire.States;

namespace Hangfire.Client
{
    /// <summary>
    /// This interface acts as extensibility point for the process
    /// of job creation. See the default implementation in the
    /// <see cref="BackgroundJobFactory"/> class.
    /// </summary>
    public interface IBackgroundJobFactory
    {
        /// <summary>
        /// Gets a state machine that's responsible for initial state change.
        /// </summary>
        [NotNull]
        IStateMachine StateMachine { get; }

        /// <summary>
        /// Runs the process of job creation with the specified context.
        /// </summary>
        [CanBeNull]
        BackgroundJob Create([NotNull] CreateContext context);
    }
}