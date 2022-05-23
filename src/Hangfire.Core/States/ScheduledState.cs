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
using System.Collections.Generic;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Newtonsoft.Json;

namespace Hangfire.States
{
    /// <summary>
    /// Defines the <i>intermediate</i> state of a background job when it is placed 
    /// on a schedule to be moved to the <see cref="EnqueuedState"/> in the future 
    /// by <see cref="DelayedJobScheduler"/> background process.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>Background job in <see cref="ScheduledState"/> is referred as
    /// <b>delayed job</b>.</para>
    /// </remarks>
    /// 
    /// <example>
    /// The following example demonstrates the creation of a background job that will
    /// be processed after two hours. Please see <see cref="O:Hangfire.BackgroundJob.Schedule">BackgroundJob.Schedule</see>
    /// and <see cref="O:Hangfire.BackgroundJobClientExtensions.Schedule">BackgroundJobClientExtensions.Schedule</see>
    /// method overloads for simpler API.
    /// 
    /// <code lang="cs" source="..\Samples\States.cs" region="ScheduledState" />
    /// </example>
    /// 
    /// <seealso cref="O:Hangfire.BackgroundJob.Schedule">BackgroundJob.Schedule Overload</seealso>
    /// <seealso cref="O:Hangfire.BackgroundJobClientExtensions.Schedule">BackgroundJobClientExtensions.Schedule Overload</seealso>
    /// <seealso cref="DelayedJobScheduler"/>
    /// <seealso cref="EnqueuedState"/>
    /// 
    /// <threadsafety static="true" instance="false"/>
    public class ScheduledState : IState
    {
        /// <summary>
        /// Represents the name of the <i>Scheduled</i> state. This field is read-only.
        /// </summary>
        /// <remarks>
        /// The value of this field is <c>"Scheduled"</c>.
        /// </remarks>
        public static readonly string StateName = "Scheduled";

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledState"/> class
        /// with the specified <i>time interval</i> after which a job should be moved to
        /// the <see cref="EnqueuedState"/>.
        /// </summary>
        /// <param name="enqueueIn">The time interval after which a job will be
        /// moved to the <see cref="EnqueuedState"/>.</param>
        public ScheduledState(TimeSpan enqueueIn)
            : this(DateTime.UtcNow.Add(enqueueIn))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledState"/>
        /// class with the specified <i>date/time in UTC format</i> when a job should 
        /// be moved to the <see cref="EnqueuedState"/>.
        /// </summary>
        /// <param name="enqueueAt">The date/time when a job will be moved to the 
        /// <see cref="EnqueuedState"/>.</param>
        [JsonConstructor]
        public ScheduledState(DateTime enqueueAt)
        {
            EnqueueAt = enqueueAt;
            ScheduledAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a date/time when a background job should be <i>enqueued</i>.
        /// </summary>
        /// <value>Any date/time in <see cref="DateTimeKind.Utc"/> format.</value>
        public DateTime EnqueueAt { get; }

        /// <summary>
        /// Gets a date/time when the current state instance was created.
        /// </summary>
        [JsonIgnore]
        public DateTime ScheduledAt { get; }

        /// <inheritdoc />
        /// <remarks>
        /// Always equals to <see cref="StateName"/> for the <see cref="ScheduledState"/>.
        /// Please see the remarks section of the <see cref="IState.Name">IState.Name</see>
        /// article for the details.
        /// </remarks>
        [JsonIgnore]
        public string Name => StateName;

        /// <inheritdoc />
        public string Reason { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false" /> for the <see cref="ScheduledState"/>.
        /// Please refer to the <see cref="IState.IsFinal">IState.IsFinal</see> documentation
        /// for the details.
        /// </remarks>
        [JsonIgnore]
        public bool IsFinal => false;

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false"/> for the <see cref="ScheduledState"/>.
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
        ///         <term><c>EnqueueAt</c></term>
        ///         <term><see cref="DateTime"/></term>
        ///         <term><see cref="JobHelper.DeserializeDateTime"/></term>
        ///         <description>Please see the <see cref="EnqueueAt"/> property.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>ScheduledAt</c></term>
        ///         <term><see cref="DateTime"/></term>
        ///         <term><see cref="JobHelper.DeserializeDateTime"/></term>
        ///         <description>Please see the <see cref="ScheduledAt"/> property.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "EnqueueAt", JobHelper.SerializeDateTime(EnqueueAt) },
                { "ScheduledAt", JobHelper.SerializeDateTime(ScheduledAt) }
            };
        }

        internal class Handler : IStateHandler
        {
            public void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                var scheduledState = context.NewState as ScheduledState;
                if (scheduledState == null)
                {
                    throw new InvalidOperationException(
                        $"`{typeof (Handler).FullName}` state handler can be registered only for the Scheduled state.");
                }

                var timestamp = JobHelper.ToTimestamp(scheduledState.EnqueueAt);
                transaction.AddToSet("schedule", context.BackgroundJob.Id, timestamp);
            }

            public void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.RemoveFromSet("schedule", context.BackgroundJob.Id);
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public string StateName => ScheduledState.StateName;
        }
    }
}
