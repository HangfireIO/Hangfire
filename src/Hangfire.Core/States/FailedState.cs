// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using System.Collections.Generic;
using Hangfire.Common;
using Newtonsoft.Json;

namespace Hangfire.States
{
    /// <summary>
    /// Defines the <i>intermediate</i> state of a background job when its processing 
    /// was interrupted by an exception and it is a developer's responsibility
    /// to decide what to do with it next.
    /// </summary>
    /// <remarks>
    /// <para>Failed state is used in Hangfire when something went wrong and an exception
    /// occurred during the background job processing. The primary reason for this state
    /// is to notify the developers that something went wrong. By default background job 
    /// is moved to the <i>Failed</i> state only after some automatic retries, because the 
    /// <see cref="AutomaticRetryAttribute"/> filter is enabled by default.</para>
    /// <note type="important">
    /// Failed jobs are <b>not expiring</b> and will stay in your current job storage 
    /// forever, increasing its size until you retry or delete them manually. If you 
    /// expect some exceptions, please use the following rules.
    /// <list type="bullet">
    ///     <item>Ignore, move to <i>Succeeded</i> state – use the <c>catch</c>
    ///     statement in your code without re-throwing the exception.</item>
    ///     <item>Ignore, move to <i>Deleted</i> state – use the <see cref="AutomaticRetryAttribute"/>
    ///     with <see cref="AttemptsExceededAction.Delete"/> option.</item>
    ///     <item>Re-queue a job – use the <see cref="AutomaticRetryAttribute"/> with
    ///     <see cref="AttemptsExceededAction.Fail"/> option.</item>
    /// </list>
    /// </note>
    /// <para>It is not supposed to use the <see cref="FailedState"/> class in a user
    /// code unless you are writing state changing filters or new background processing
    /// rules.</para>
    /// </remarks>
    /// 
    /// <seealso cref="AutomaticRetryAttribute"/>
    /// <seealso cref="IBackgroundJobStateChanger"/>
    /// <seealso cref="Server.Worker"/>
    /// 
    /// <threadsafety static="true" instance="false" />
    public class FailedState : IState
    {
        internal static int? MaxLinesInExceptionDetails = 100;

        /// <summary>
        /// Represents the name of the <i>Failed</i> state. This field is read-only.
        /// </summary>
        /// <remarks>
        /// The value of this field is <c>"Failed"</c>.
        /// </remarks>
        public static readonly string StateName = "Failed";

        /// <summary>
        /// Initializes a new instace of the <see cref="FailedState"/> class
        /// with the given exception.
        /// </summary>
        /// <param name="exception">Exception that occurred during the background 
        /// job processing.</param>
        /// 
        /// <exception cref="ArgumentNullException">The <paramref name="exception"/> 
        /// argument is <see langword="null" /></exception>
        public FailedState(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            FailedAt = DateTime.UtcNow;
            Exception = exception;
        }

        /// <summary>
        /// Gets a date/time when the current state instance was created.
        /// </summary>
        [JsonIgnore]
        public DateTime FailedAt { get; }

        /// <summary>
        /// Gets the exception that occurred during the background job processing.
        /// </summary>
        public Exception Exception { get; }

        /// <inheritdoc />
        /// <remarks>
        /// Always equals to <see cref="StateName"/> for the <see cref="FailedState"/>.
        /// Please see the remarks section of the <see cref="IState.Name">IState.Name</see>
        /// article for the details.
        /// </remarks>
        [JsonIgnore]
        public string Name => StateName;

        /// <inheritdoc />
        public string Reason { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false" /> for the <see cref="FailedState"/>.
        /// Please refer to the <see cref="IState.IsFinal">IState.IsFinal</see> documentation
        /// for the details.
        /// </remarks>
        [JsonIgnore]
        public bool IsFinal => false;

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false"/> for the <see cref="FailedState"/>.
        /// Please see the description of this property in the
        /// <see cref="IState.IgnoreJobLoadException">IState.IgnoreJobLoadException</see>
        /// article.
        /// </remarks>
        [JsonIgnore]
        public bool IgnoreJobLoadException => false;

        /// <inheritdoc />
        /// <remarks>
        /// <para>Returning dictionary contains the following keys. You can obtain 
        /// the state data by using the <see cref="Storage.IStorageConnection.GetStateData"/>
        /// method.</para>
        /// <list type="table">
        ///     <listheader>
        ///         <term>Key</term>
        ///         <term>Type</term>
        ///         <term>Deserialize Method</term>
        ///         <description>Notes</description>
        ///     </listheader>
        ///     <item>
        ///         <term><c>FailedAt</c></term>
        ///         <term><see cref="DateTime"/></term>
        ///         <term><see cref="JobHelper.DeserializeDateTime"/></term>
        ///         <description>Please see the <see cref="FailedAt"/> property.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>ExceptionType</c></term>
        ///         <term><see cref="string"/></term>
        ///         <term><i>Not required</i></term>
        ///         <description>The full name of the current exception type.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>ExceptionMessage</c></term>
        ///         <term><see cref="string"/></term>
        ///         <term><i>Not required</i></term>
        ///         <description>Message that describes the current exception.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>ExceptionDetails</c></term>
        ///         <term><see cref="string"/></term>
        ///         <term><i>Not required</i></term>
        ///         <description>String representation of the current exception.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "FailedAt", JobHelper.SerializeDateTime(FailedAt) },
                { "ExceptionType", Exception.GetType().FullName },
                { "ExceptionMessage", Exception.Message },
                { "ExceptionDetails", Exception.ToStringWithOriginalStackTrace(MaxLinesInExceptionDetails) }
            };
        }
    }
}
