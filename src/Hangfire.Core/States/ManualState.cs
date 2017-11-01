using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Storage;

namespace Hangfire.States
{
    /// <summary>
    /// Defines the <i>intermediate</i> state of a background job when it is placed 
    /// on manual state to be triggered manually and immediately will be moved to the <see cref="EnqueuedState"/>.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>Background job in <see cref="ManualState"/> is referred as
    /// <b>Manual job</b>.</para>
    /// </remarks>
    /// 
    /// <example>
    /// The following example demonstrates the creation of a background job that will
    /// be processed after manually triggered. Please see <see cref="O:Hangfire.BackgroundJob.Stash">BackgroundJob.Stash</see>
    /// and <see cref="O:Hangfire.BackgroundJobClientExtensions.Stash">BackgroundJobClientExtensions.Stash</see>
    /// method overloads for simpler API.
    /// 
    /// <code lang="cs" source="..\Samples\States.cs" region="ManualState" />
    /// </example>
    /// 
    /// <seealso cref="O:Hangfire.BackgroundJob.Stash">BackgroundJob.Stash Overload</seealso>
    /// <seealso cref="O:Hangfire.BackgroundJobClientExtensions.Stash">BackgroundJobClientExtensions.Schedule Stash</seealso>    
    /// <seealso cref="EnqueuedState"/>
    /// 
    /// <threadsafety static="true" instance="false"/>    
    public class ManualState : IState
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
        public static readonly string StateName = "Manual";

        private string _queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualState"/> class 
        /// with the <see cref="DefaultQueue">default</see> queue name.
        /// </summary>
        public ManualState()
            : this(DefaultQueue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualState"/> class
        /// with the specified queue name.
        /// </summary>
        /// <param name="queue">The queue name to which a background job identifier will be added.</param>
        /// 
        /// <seealso cref="Queue"/>
        /// 
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="queue"/> argument is <see langword="null"/>,  empty or consist only of 
        /// white-space characters.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The <paramref name="queue"/> argument is not a valid queue name.
        /// </exception>
        public ManualState([NotNull] string queue)
        {
            ValidateQueueName("queue", queue);

            _queue = queue;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets or sets a queue name to which a background job identifier
        /// will be added.
        /// </summary>
        /// <value>A queue name that consist only of lowercase letters, digits and
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
        /// empty or consist only of white-space characters.
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
                ValidateQueueName("value", value);
                _queue = value;
            }
        }

        /// <summary>
        /// Gets a date/time when the current state instance was created.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <inheritdoc />
        /// <remarks>
        /// Always equals to <see cref="StateName"/> for the <see cref="ManualState"/>.
        /// Please see the remarks section of the <see cref="IState.Name">IState.Name</see>
        /// article for the details.
        /// </remarks>
        public string Name { get { return StateName; } }

        /// <inheritdoc />
        public string Reason { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false" /> for the <see cref="ManualState"/>.
        /// Please refer to the <see cref="IState.IsFinal">IState.IsFinal</see> documentation
        /// for the details.
        /// </remarks>
        public bool IsFinal { get { return false; } }

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false"/> for the <see cref="ManualState"/>.
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
        ///         <term><c>CreatedAt</c></term>
        ///         <term><see cref="DateTime"/></term>
        ///         <term><see cref="JobHelper.DeserializeDateTime"/></term>
        ///         <description>Please see the <see cref="CreatedAt"/> property.</description>
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
                { "CreatedAt", JobHelper.SerializeDateTime(CreatedAt) },
                { "Queue", Queue }
            };
        }

        internal static void ValidateQueueName([InvokerParameterName] string parameterName, string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(parameterName);
            }

            if (!Regex.IsMatch(value, @"^[a-z0-9_]+$"))
            {
                throw new ArgumentException(
                    String.Format(
                        "The queue name must consist of lowercase letters, digits and underscore characters only. Given: '{0}'.",
                        value),
                    parameterName);
            }
        }

        internal class Handler : IStateHandler
        {
            public void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                var manualState = context.NewState as ManualState;
                if (manualState == null)
                {
                    throw new InvalidOperationException(String.Format(
                        "`{0}` state handler can be registered only for the Manual state.",
                        typeof(Handler).FullName));
                }

                transaction.AddToQueue(manualState.Queue, context.BackgroundJob.Id);
            }

            public void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
            }

            public string StateName
            {
                get { return ManualState.StateName; }
            }
        }
    }
}
