using System;
using System.Collections.Generic;
using Hangfire.Common;
using Hangfire.Storage;

namespace Hangfire.States
{
    public class SkippedState : IState
    {
        public SkippedState()
        {
            SkippedAt = DateTime.UtcNow; 
        }
        /// <summary>
        /// Represents the name of the <i>Succeeded</i> state. This field is read-only.
        /// </summary>
        /// <remarks>
        /// The value of this field is <c>"Succeeded"</c>.
        /// </remarks>
        public static readonly string StateName = "Skipped";


        public DateTime SkippedAt { get; }

        /// <inheritdoc />
        /// <remarks>
        /// Always equals to <see cref="StateName"/> for the <see cref="SkippedState"/>.
        /// Please see the remarks section of the <see cref="IState.Name">IState.Name</see>
        /// article for the details.
        /// </remarks>
        public string Name => StateName;

        /// <inheritdoc />
        public string Reason { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="true"/> for the <see cref="SkippedState"/>.
        /// Please refer to the <see cref="IState.IsFinal">IState.IsFinal</see> documentation
        /// for the details.
        /// </remarks>
        public bool IsFinal => true;

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false" /> for the <see cref="SkippedState"/>.
        /// Please see the description of this property in the
        /// <see cref="IState.IgnoreJobLoadException">IState.IgnoreJobLoadException</see>
        /// article.
        /// </remarks>
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
        ///         <term><c>SkippedAt</c></term>
        ///         <term><see cref="DateTime"/></term>
        ///         <term><see cref="JobHelper.DeserializeDateTime"/></term>
        ///         <description>Please see the <see cref="SkippedAt"/> property.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        public Dictionary<string, string> SerializeData()
        {
            var data = new Dictionary<string, string>
            {
                { "SkippedAt",  JobHelper.SerializeDateTime(SkippedAt) }
            };

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

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public string StateName => SkippedState.StateName;
        }
    }
}
