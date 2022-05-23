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
using System.Text.RegularExpressions;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Storage;
using Newtonsoft.Json;

namespace Hangfire.States
{
    /// <summary>
    /// Defines the <i>intermediate</i> state of a background job when it is placed 
    /// on a message queue to be processed by the <see cref="Server.Worker"/> 
    /// background process <i>as soon as possible</i>.
    /// </summary>
    /// <remarks>
    /// <para>Background job in <see cref="EnqueuedState"/> is referred as
    /// <b>fire-and-forget job</b>.</para>
    /// <para>Background job identifier is placed on a queue with the given name. When
    /// a queue name wasn't specified, the <see cref="DefaultQueue"/> name will
    /// be used. Message queue implementation depends on a current <see cref="JobStorage"/>
    /// instance.</para>
    /// </remarks> 
    /// <example>
    /// The following example demonstrates the creation of a background job in
    /// <see cref="EnqueuedState"/>. Please see 
    /// <see cref="O:Hangfire.BackgroundJob.Enqueue">BackgroundJob.Enqueue</see>
    /// and <see cref="O:Hangfire.BackgroundJobClientExtensions.Enqueue">BackgroundJobClientExtensions.Enqueue</see>
    /// method overloads for simpler API.
    /// 
    /// <code lang="cs" source="..\Samples\States.cs" region="EnqueuedState #1" />
    /// <code lang="vb" source="..\VBSamples\States.vb" region="EnqueuedState #1" />
    /// 
    /// The code below implements the retry action for a failed background job.
    /// 
    /// <code lang="cs" source="..\Samples\States.cs" region="EnqueuedState #2" />
    /// <code lang="vb" source="..\VBSamples\States.vb" region="EnqueuedState #2" />
    ///  
    /// </example>
    /// 
    /// <seealso cref="O:Hangfire.BackgroundJob.Enqueue">BackgroundJob.Enqueue Overload</seealso>
    /// <seealso cref="O:Hangfire.BackgroundJobClientExtensions.Enqueue">BackgroundJobClientExtensions.Enqueue Overload</seealso>
    /// <seealso cref="O:Hangfire.BackgroundJobClientExtensions.Create">BackgroundJobClientExtensions.Create Overload</seealso>
    /// <seealso cref="IBackgroundJobClient.Create" />
    /// <seealso cref="IBackgroundJobClient.ChangeState" />
    /// <seealso cref="Server.Worker"/>
    /// 
    /// <threadsafety static="true" instance="false" />
    public class EnqueuedState : IState
    {
        /// <summary>
        /// Represents the default queue name. This field is constant.
        /// </summary>
        /// <remarks>
        /// The value of this field is <c>"default"</c>.
        /// </remarks>
        public const string DefaultQueue = "default";

        /// <summary>
        /// Represents the name of the <i>Enqueued</i> state. This field is read-only.
        /// </summary>
        /// <remarks>
        /// The value of this field is <c>"Enqueued"</c>.
        /// </remarks>
        public static readonly string StateName = "Enqueued";

        private string _queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnqueuedState"/> class 
        /// with the <see cref="DefaultQueue">default</see> queue name.
        /// </summary>
        public EnqueuedState()
            : this(DefaultQueue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnqueuedState"/> class
        /// with the specified queue name.
        /// </summary>
        /// <param name="queue">The queue name to which a background job identifier will be added.</param>
        /// 
        /// <seealso cref="Queue"/>
        /// 
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="queue"/> argument is <see langword="null"/>, empty or consists only of 
        /// white-space characters.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <paramref name="queue"/> argument is not a valid queue name.
        /// </exception>
        [JsonConstructor]
        public EnqueuedState([CanBeNull] string queue)
        {
            queue = queue ?? DefaultQueue;

            ValidateQueueName(nameof(queue), queue);

            _queue = queue;
            EnqueuedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets or sets a queue name to which a background job identifier
        /// will be added.
        /// </summary>
        /// <value>A queue name that consists only of lowercase letters, digits and
        /// underscores.</value>
        /// <remarks>
        /// <para>Queue name must consist only of lowercase letters, digits and
        /// underscores, other characters aren't permitted. Some examples:</para>
        /// <list type="bullet">
        ///     <item><c>"critical"</c> (good)</item>
        ///     <item><c>"worker_1"</c> (good)</item>
        ///     <item><c>"documents queue"</c> (bad, whitespace)</item>
        ///     <item><c>"MyQueue"</c> (bad, capital letters)</item>
        /// </list>
        /// </remarks>
        /// 
        /// <exception cref="ArgumentNullException">
        /// The value specified for a set operation is <see langword="null"/>, 
        /// empty or consists only of white-space characters.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The value specified for a set operation is not a valid queue name.
        /// </exception>
        [NotNull]
        public string Queue
        {
            get { return _queue; }
            set
            {
                ValidateQueueName(nameof(value), value);
                _queue = value;
            }
        }

        /// <summary>
        /// Gets a date/time when the current state instance was created.
        /// </summary>
        [JsonIgnore]
        public DateTime EnqueuedAt { get; }

        /// <inheritdoc />
        /// <remarks>
        /// Always equals to <see cref="StateName"/> for the <see cref="EnqueuedState"/>.
        /// Please see the remarks section of the <see cref="IState.Name">IState.Name</see>
        /// article for the details.
        /// </remarks>
        [JsonIgnore]
        public string Name => StateName;

        /// <inheritdoc />
        public string Reason { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false" /> for the <see cref="EnqueuedState"/>.
        /// Please refer to the <see cref="IState.IsFinal">IState.IsFinal</see> documentation
        /// for the details.
        /// </remarks>
        [JsonIgnore]
        public bool IsFinal => false;

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false"/> for the <see cref="EnqueuedState"/>.
        /// Please see the description of this property in the
        /// <see cref="IState.IgnoreJobLoadException">IState.IgnoreJobLoadException</see>
        /// article.
        /// </remarks>
        [JsonIgnore]
        public bool IgnoreJobLoadException => false;

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
        ///         <term><c>EnqueuedAt</c></term>
        ///         <term><see cref="DateTime"/></term>
        ///         <term><see cref="JobHelper.DeserializeDateTime"/></term>
        ///         <description>Please see the <see cref="EnqueuedAt"/> property.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>Queue</c></term>
        ///         <term><see cref="string"/></term>
        ///         <term><i>Not required</i></term>
        ///         <description>Please see the <see cref="Queue"/> property.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "EnqueuedAt", JobHelper.SerializeDateTime(EnqueuedAt) },
                { "Queue", Queue }
            };
        }

        internal static void ValidateQueueName([InvokerParameterName] string parameterName, string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(parameterName);
            }

            if (!Regex.IsMatch(value, @"^[a-z0-9_-]+$"))
            {
                throw new ArgumentException(
                    $"The queue name must consist of lowercase letters, digits, underscore, and dash characters only. Given: '{value}'.",
                    parameterName);
            }
        }

        internal class Handler : IStateHandler
        {
            public void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                var enqueuedState = context.NewState as EnqueuedState;
                if (enqueuedState == null)
                {
                    throw new InvalidOperationException(
                        $"`{typeof (Handler).FullName}` state handler can be registered only for the Enqueued state.");
                }

                transaction.AddToQueue(enqueuedState.Queue, context.BackgroundJob.Id);
            }

            public void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public string StateName => EnqueuedState.StateName;
        }
    }
}
