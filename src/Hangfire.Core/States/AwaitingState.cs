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
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Storage;
using Newtonsoft.Json;

namespace Hangfire.States
{
    /// <summary>
    /// Defines the <i>intermediate</i> state of a background job when it is waiting
    /// for a parent background job to be finished before it is moved to the
    /// <see cref="EnqueuedState"/> by the <see cref="ContinuationsSupportAttribute"/>
    /// filter.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>Background job in <see cref="AwaitingState"/> is referred as a
    /// <b>continuation</b> of a background job with <see cref="ParentId"/>.</para>
    /// </remarks>
    /// 
    /// <threadsafety static="true" instance="false"/>
    public class AwaitingState : IState
    {
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromDays(365);

        /// <summary>
        /// Represents the name of the <i>Awaiting</i> state. This field is read-only.
        /// </summary>
        /// <remarks>
        /// The value of this field is <c>"Awaiting"</c>.
        /// </remarks>
        public static readonly string StateName = "Awaiting";
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AwaitingState"/> class with
        /// the specified parent background job id and with an instance of the 
        /// <see cref="EnqueuedState"/> class as a next state.
        /// </summary>
        /// <param name="parentId">The identifier of a background job to wait for.</param>
        public AwaitingState([NotNull] string parentId)
            : this(parentId, new EnqueuedState())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AwaitingState"/> class with
        /// the specified parent job id and next state.
        /// </summary>
        /// <param name="parentId">The identifier of a background job to wait for.</param>
        /// <param name="nextState">The next state for the continuation.</param>
        public AwaitingState([NotNull] string parentId, [NotNull] IState nextState)
            : this(parentId, nextState, JobContinuationOptions.OnAnyFinishedState)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AwaitingState"/> class with
        /// the given <i>options</i> along with other parameters.
        /// </summary>
        /// <param name="parentId">The identifier of a background job to wait for.</param>
        /// <param name="nextState">The next state for the continuation.</param>
        /// <param name="options">Options to configure a continuation.</param>
        [JsonConstructor]
        public AwaitingState([NotNull] string parentId, [NotNull] IState nextState, JobContinuationOptions options)
            : this(parentId, nextState, options, DefaultExpiration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AwaitingState"/> class with
        /// the specified <i>expiration time</i> along with other parameters.
        /// </summary>
        /// <param name="parentId">The identifier of a background job to wait for.</param>
        /// <param name="nextState">The next state for the continuation.</param>
        /// <param name="options">Options to configure the continuation.</param>
        /// <param name="expiration">The expiration time for the continuation.</param>
        public AwaitingState(
            [NotNull] string parentId,
            [NotNull] IState nextState,
            JobContinuationOptions options,
            TimeSpan expiration)
        {
            if (parentId == null) throw new ArgumentNullException(nameof(parentId));
            if (nextState == null) throw new ArgumentNullException(nameof(nextState));

            ParentId = parentId;
            NextState = nextState;

            Options = options;
            Expiration = expiration;
        }

        /// <summary>
        /// Gets the identifier of a parent background job.
        /// </summary>
        [NotNull]
        public string ParentId { get; }

        /// <summary>
        /// Gets the next state, to which a background job will be moved.
        /// </summary>
        [NotNull]
        public IState NextState { get; }

        /// <summary>
        /// Gets the continuation options associated with the current state.
        /// </summary>
        public JobContinuationOptions Options { get; }

        /// <summary>
        /// Gets the expiration time of a background job continuation.
        /// </summary>
        [JsonIgnore]
        public TimeSpan Expiration { get; }

        /// <inheritdoc />
        /// <remarks>
        /// Always equals to <see cref="StateName"/> for the <see cref="AwaitingState"/>.
        /// Please see the remarks section of the <see cref="IState.Name">IState.Name</see>
        /// article for the details.
        /// </remarks>
        [JsonIgnore]
        public string Name => StateName;

        /// <inheritdoc />
        public string Reason { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false"/> for the <see cref="AwaitingState"/>.
        /// Please refer to the <see cref="IState.IsFinal">IState.IsFinal</see> documentation
        /// for the details.
        /// </remarks>
        [JsonIgnore]
        public bool IsFinal => false;

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false" /> for the <see cref="AwaitingState"/>.
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
        ///         <term><c>ParentId</c></term>
        ///         <term><see cref="string"/></term>
        ///         <term><i>Not required</i></term>
        ///         <description>Please see the <see cref="ParentId"/> property.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>NextState</c></term>
        ///         <term><see cref="IState"/></term>
        ///         <term>
        ///             <see cref="SerializationHelper.Deserialize{T}(string, SerializationOption)"/> with 
        ///             <see cref="SerializationOption.TypedInternal"/>
        ///         </term>
        ///         <description>Please see the <see cref="NextState"/> property.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>Options</c></term>
        ///         <term><see cref="JobContinuationOptions"/></term>
        ///         <term>
        ///             <see cref="Enum.Parse(Type, string)"/> with <see cref="JobContinuationOptions"/>
        ///         </term>
        ///         <description>Please see the <see cref="Options"/> property.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        public Dictionary<string, string> SerializeData()
        {
            var result = new Dictionary<string, string>
            {
                { "ParentId", ParentId },
                { "NextState", SerializationHelper.Serialize(NextState, SerializationOption.TypedInternal) }
            };

            if (GlobalConfiguration.HasCompatibilityLevel(CompatibilityLevel.Version_170))
            {
                result.Add("Options", Options.ToString("D"));
            }
            else
            {
                result.Add("Options", Options.ToString("G"));
                result.Add("Expiration", Expiration.ToString());
            }

            return result;
        }

        internal class Handler : IStateHandler
        {
            public void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.AddToSet("awaiting", context.BackgroundJob.Id, JobHelper.ToTimestamp(DateTime.UtcNow));
            }

            public void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                transaction.RemoveFromSet("awaiting", context.BackgroundJob.Id);
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public string StateName => AwaitingState.StateName;
        }
    }
}