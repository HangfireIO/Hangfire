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
using System.Globalization;
using Hangfire.Common;
using Hangfire.Storage;

namespace Hangfire.States
{
    /// <summary>
    /// Defines the <i>final</i> state of a background job when a <see cref="Server.Worker"/>
    /// performed an <i>enqueued</i> job without any exception thrown during the performance.
    /// </summary>
    /// <remarks>
    /// <para>All the transitions to the <i>Succeeded</i> state are internal for the <see cref="Server.Worker"/>
    /// background process. You can't create background jobs using this state, and can't change state
    /// to <i>Succeeded</i>.</para>
    /// <para>This state is used in a user code primarily in state change filters (TODO: add a link)
    /// to add custom logic during state transitions.</para> 
    /// </remarks> 
    /// 
    /// <seealso cref="EnqueuedState"/>
    /// <seealso cref="Server.Worker"/>
    /// <seealso cref="IState"/>
    /// 
    /// <threadsafety static="true" instance="false" />
    public class SucceededState : IState
    {
        /// <summary>
        /// Represents the name of the <i>Succeeded</i> state. This field is read-only.
        /// </summary>
        /// <remarks>
        /// The value of this field is <c>"Succeeded"</c>.
        /// </remarks>
        public static readonly string StateName = "Succeeded";

        internal SucceededState(object result, long latency, long performanceDuration)
        {
            SucceededAt = DateTime.UtcNow;
            Result = result;
            Latency = latency;
            PerformanceDuration = performanceDuration;
            
        }

        /// <summary>
        /// Gets a date/time when the current state instance was created.
        /// </summary>
        public DateTime SucceededAt { get; private set; }

        /// <summary>
        /// Gets the value returned by a job method.
        /// </summary>
        public object Result { get; private set; }
        
        /// <summary>
        /// Gets the total number of milliseconds passed from a job
        /// creation time till the start of the performance.
        /// </summary>
        public long Latency { get; private set; }

        /// <summary>
        /// Gets the total milliseconds elapsed from a processing start.
        /// </summary>
        public long PerformanceDuration { get; private set; }

        /// <inheritdoc />
        /// <remarks>
        /// Always equals to <see cref="StateName"/> for the <see cref="SucceededState"/>.
        /// Please see the remarks section of the <see cref="IState.Name">IState.Name</see>
        /// article for the details.
        /// </remarks>
        public string Name { get { return StateName; } }

        /// <inheritdoc />
        public string Reason { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="true"/> for the <see cref="SucceededState"/>.
        /// Please refer to the <see cref="IState.IsFinal">IState.IsFinal</see> documentation
        /// for the details.
        /// </remarks>
        public bool IsFinal { get { return true; } }

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false" /> for the <see cref="SucceededState"/>.
        /// Please see the description of this property in the
        /// <see cref="IState.IgnoreJobLoadException">IState.IgnoreJobLoadException</see>
        /// article.
        /// </remarks>
        public bool IgnoreJobLoadException { get { return false; } }

        /// <inheritdoc />
        /// <remarks>
        /// <para>Returning dictionary contains the following keys. You can obtain 
        /// the state data by using the <see cref="IStorageConnection.GetStateData"/>
        /// method.</para>
        /// <list type="table">
        ///     <listheader>
        ///         <term>Key</term>
        ///         <term>Type</term>
        ///         <term>Deserialize Method</term>
        ///         <description>Notes</description>
        ///     </listheader>
        ///     <item>
        ///         <term><c>SucceededAt</c></term>
        ///         <term><see cref="DateTime"/></term>
        ///         <term><see cref="JobHelper.DeserializeDateTime"/></term>
        ///         <description>Please see the <see cref="SucceededAt"/> property.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>PerformanceDuration</c></term>
        ///         <term><see cref="long"/></term>
        ///         <term>
        ///             <see cref="Int64.Parse(string, IFormatProvider)"/> with 
        ///             <see cref="CultureInfo.InvariantCulture"/>
        ///         </term>
        ///         <description>Please see the <see cref="PerformanceDuration"/> property.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>Latency</c></term>
        ///         <term><see cref="long"/></term>
        ///         <term>
        ///             <see cref="Int64.Parse(string, IFormatProvider)"/> with 
        ///             <see cref="CultureInfo.InvariantCulture"/>
        ///         </term>
        ///         <description>Please see the <see cref="Latency"/> property.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>Result</c></term>
        ///         <term><see cref="object"/></term>
        ///         <term><see cref="JobHelper.FromJson"/></term>
        ///         <description>
        ///             <para>Please see the <see cref="Result"/> property.</para>
        ///             <para>This key may be missing from the dictionary, when the return 
        ///             value was <see langword="null" />. Always check for its existence 
        ///             before using it.</para>
        ///         </description>
        ///     </item>
        /// </list>
        /// </remarks>
        public Dictionary<string, string> SerializeData()
        {
            var data = new Dictionary<string, string>
            {
                { "SucceededAt",  JobHelper.SerializeDateTime(SucceededAt) },
                { "PerformanceDuration", PerformanceDuration.ToString(CultureInfo.InvariantCulture) },
                { "Latency", Latency.ToString(CultureInfo.InvariantCulture) }
            };

            if (Result != null)
            {
                string serializedResult;

                try
                {
                    serializedResult = JobHelper.ToJson(Result);
                }
                catch (Exception)
                {
                    serializedResult = "Can not serialize the return value";
                }

                if (serializedResult != null)
                {
                    data.Add("Result", serializedResult);
                }
            }

            return data;
        }

        internal class Handler : IStateHandler
        {
            public void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.IncrementCounter("stats:succeeded");
            }

            public void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.DecrementCounter("stats:succeeded");
            }

            public string StateName
            {
                get { return SucceededState.StateName; }
            }
        }
    }
}
