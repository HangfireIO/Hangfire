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
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;

namespace Hangfire
{
    /// <summary>
    /// Provides methods for creating background jobs and changing their states.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>Please see the <see cref="BackgroundJobClient"/> class for
    /// details regarding the implementation.</para>
    /// </remarks>
    public interface IBackgroundJobClient
    {
        /// <summary>
        /// Creates a new background job in a specified state.
        /// </summary>
        /// 
        /// <param name="job">Job that should be processed in background.</param>
        /// <param name="state">Initial state for a background job.</param>
        /// <returns>Unique identifier of a created background job <i>-or-</i> 
        ///  <see langword="null"/>, if it was not created.</returns>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="job"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        /// <exception cref="BackgroundJobClientException">Creation failed due to an exception.</exception>
        /// 
        /// <remarks>
        /// <para>The interface allows implementations to return <see langword="null"/> 
        /// value for this method when background job creation has been canceled
        /// by an implementation under the normal circumstances (not due to an
        /// exception). For example, the <see cref="CreatingContext"/> class
        /// contains the <see cref="CreatingContext.Canceled"/> property that
        /// may be used by a client filter to cancel a background job creation.
        /// </para>
        /// 
        /// <para>The interface allows implementations to create a background 
        /// job in a state other than specified. The given state instance also 
        /// may be modified. For example, <see cref="ElectStateContext"/> class
        /// contains public setter for the <see cref="ElectStateContext.CandidateState"/>
        /// property allowing to choose completely different state by state
        /// election filters.</para>
        /// </remarks>
        [CanBeNull]
        string Create([NotNull] Job job, [NotNull] IState state);

        /// <summary>
        /// Attempts to change a state of a background job with a given
        /// identifier to a specified one.
        /// </summary>
        /// 
        /// <param name="jobId">Identifier of background job, whose state should be changed.</param>
        /// <param name="state">New state for a background job.</param>
        /// <param name="expectedState">Expected state assertion, or <see langword="null"/> if unneeded.</param>
        /// 
        /// <returns><see langword="true"/>, if a <b>given</b> state was applied
        /// successfully otherwise <see langword="false"/>.</returns>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="jobId"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        /// <exception cref="BackgroundJobClientException">State change failed due to an exception.</exception>
        /// 
        /// <remarks>
        /// <para>If <paramref name="expectedState"/> value is not null, state 
        /// change will be performed only if the current state name of a job 
        /// equal to the given value.</para>
        /// 
        /// <para>The interface allows implementations to change a state of a 
        /// background job to other than specified. The given state instance also
        /// may be modified. For example, <see cref="ElectStateContext"/> class
        /// contains public setter for the <see cref="ElectStateContext.CandidateState"/>
        /// property allowing to choose completely different state by state
        /// election filters. If a state was changed, <see langword="false"/> 
        /// value will be returned.</para>
        /// </remarks>
        bool ChangeState([NotNull] string jobId, [NotNull] IState state, [CanBeNull] string expectedState);
    }
}