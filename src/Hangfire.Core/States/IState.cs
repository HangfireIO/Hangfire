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

using System.Collections.Generic;
using Hangfire.Annotations;

namespace Hangfire.States
{
    /// <summary>
    /// Provides the essential members for describing a background job state.
    /// </summary>
    /// <remarks>
    /// <para>Background job processing in Hangfire is all about moving a background job
    /// from one state to another. States are used to clearly decide what to do
    /// with a background job. For example, <see cref="EnqueuedState"/> tells
    /// Hangfire that a job should be processed by a <see cref="Hangfire.Server.Worker"/>,
    /// and <see cref="FailedState"/> tells Hangfire that a job should be investigated 
    /// by a developer.</para>
    /// 
    /// <para>Each state has some essential properties like <see cref="Name"/>,
    /// <see cref="IsFinal"/> and custom ones that are exposed through
    /// the <see cref="SerializeData"/> method. Serialized data may be used during
    /// the processing stage.</para>
    /// 
    /// <para>Hangfire allows you to define custom states to extend the processing
    /// pipeline. <see cref="IStateHandler"/> interface implementation can be used
    /// to define additional work for a state transition, and 
    /// <see cref="Server.IBackgroundProcess"/> interface implementation can be
    /// used to process background jobs in a new state. For example, delayed jobs
    /// and their <see cref="ScheduledState"/>, continuations and their 
    /// <see cref="AwaitingState"/> can be simply moved to an extension package.</para>
    /// </remarks>
    /// 
    /// <example>
    /// <para>Let's create a new state. Consider you have background jobs that
    /// throw a transient exception from time to time, and you want to simply
    /// ignore those exceptions. By default, Hangfire will move a job that threw
    /// an exception to the <see cref="FailedState"/>, however a job in the <i>failed</i>
    /// state will live in a Failed jobs page forever, unless we use <see cref="AutomaticRetryAttribute"/>,
    /// delete or retry it manually, because the <see cref="FailedState"/> is not
    /// a <i>final</i> state.</para>
    /// 
    /// <para>Our new state will look like a <see cref="FailedState"/>, but we
    /// define the state as a <i>final</i> one, letting Hangfire to expire faulted
    /// jobs. Please refer to the <see cref="IState"/> interface properties to learn
    /// about their details.</para>
    /// 
    /// <para>In articles related to <see cref="IStateHandler"/> and <see cref="IElectStateFilter"/>
    /// interfaces we'll discuss how to use this new state.</para>
    /// 
    /// <code lang="cs" source="..\Samples\States.cs" region="FaultedState" />
    /// </example>
    /// 
    /// <seealso cref="IBackgroundJobStateChanger" />
    /// <seealso cref="IStateHandler" />
    /// <seealso cref="IElectStateFilter" />
    /// <seealso cref="IApplyStateFilter" />
    public interface IState
    {
        /// <summary>
        /// Gets the unique name of the state.
        /// </summary>
        /// 
        /// <value>Unique among other states string, that is ready for 
        /// ordinal comparisons.</value>
        /// 
        /// <remarks>
        /// <para>The state name is used to differentiate one state from another
        /// during the state change process. So all the implemented states
        /// should have a <b>unique</b> state name. Please use one-word names 
        /// that start with a capital letter, in a past tense in English for 
        /// your state names, for example:</para>
        /// <list type="bullet">
        ///     <item><c>Succeeded</c></item>
        ///     <item><c>Enqueued</c></item>
        ///     <item><c>Deleted</c></item>
        ///     <item><c>Failed</c></item>
        /// </list>
        /// 
        /// <note type="implement">
        /// The returning value should be hard-coded, no modifications of
        /// this property should be allowed to a user. Implementors should
        /// not add a public setter on this property.
        /// </note>
        /// </remarks>
        [NotNull] string Name { get; }

        /// <summary>
        /// Gets the human-readable reason of a state transition.
        /// </summary>
        /// 
        /// <value>Any string with a reasonable length to fit dashboard elements.</value>
        /// 
        /// <remarks>
        /// <para>The reason is usually displayed in the Dashboard UI to simplify 
        /// the understanding of a background job lifecycle by providing a 
        /// human-readable text that explains why a background job is moved
        /// to the corresponding state. Here are some examples:</para>
        /// <list type="bullet">
        ///     <item>
        ///         <i>Can not change the state to 'Enqueued': target 
        ///         method was not found</i>
        ///     </item>
        ///     <item><i>Exceeded the maximum number of retry attempts</i></item>
        /// </list>
        /// <note type="implement">
        /// The reason value is usually not hard-coded in a state implementation,
        /// allowing users to change it when creating an instance of a state 
        /// through the public setter.
        /// </note>
        /// </remarks>
        [CanBeNull] string Reason { get; }

        /// <summary>
        /// Gets if the current state is a <i>final</i> one.
        /// </summary>
        /// 
        /// <value><see langword="false" /> for <i>intermediate states</i>,
        /// and <see langword="true" /> for the <i>final</i> ones.</value>
        /// 
        /// <remarks>
        /// <para>Final states define a termination stage of a background job 
        /// processing pipeline. Background jobs in a final state is considered 
        /// as finished with no further processing required.</para>
        /// 
        /// <para>The <see cref="IBackgroundJobStateChanger">state machine</see> marks
        /// finished background jobs to be expired within an interval that
        /// is defined in the <see cref="ApplyStateContext.JobExpirationTimeout"/>
        /// property that is available from a state changing filter that 
        /// implements the <see cref="IApplyStateFilter"/> interface.</para>
        /// 
        /// <note type="implement">
        /// When implementing this property, always hard-code this property to
        /// <see langword="true"/> or <see langword="false" />. Hangfire does
        /// not work with states that can be both <i>intermediate</i> and
        /// <i>final</i> yet. Don't define a public setter for this property.
        /// </note>
        /// </remarks>
        /// 
        /// <seealso cref="SucceededState" />
        /// <seealso cref="FailedState" />
        /// <seealso cref="DeletedState" />
        bool IsFinal { get; }

        /// <summary>
        /// Gets whether transition to this state should ignore job de-serialization 
        /// exceptions.
        /// </summary>
        /// 
        /// <value><see langword="false"/> to move to the <see cref="FailedState"/> on 
        /// deserialization exceptions, <see langword="true" /> to ignore them.</value>
        /// 
        /// <remarks>
        /// <para>During a state transition, an instance of the <see cref="Common.Job"/> class
        /// is deserialized to get state changing filters, and to allow <see cref="IStateHandler">
        /// state handlers</see> to perform additional work related to the state.</para>
        /// 
        /// <para>However we cannot always deserialize a job, for example, when job method was
        /// removed from the code base or its assembly reference is missing. Since background
        /// processing is impossible anyway, the <see cref="IBackgroundJobStateChanger">state machine</see>
        /// moves such a background job to the <see cref="FailedState"/> in this case to
        /// highlight a problem to the developers (because deserialization exception may
        /// occur due to bad refactorings or other programming mistakes).</para>
        /// 
        /// <para>However, in some exceptional cases we can ignore deserialization exceptions,
        /// and allow a state transition for some states that does not require a <see cref="Common.Job"/>
        /// instance. <see cref="FailedState"/> itself and <see cref="DeletedState"/> are
        /// examples of such a behavior.</para>
        /// 
        /// <note type="implement">
        /// In general, implementers should return <see langword="false"/> when implementing 
        /// this property.
        /// </note>
        /// </remarks>
        /// 
        /// <seealso cref="FailedState"/>
        /// <seealso cref="DeletedState"/>
        bool IgnoreJobLoadException { get; }

        /// <summary>
        /// Gets a serialized representation of the current state. 
        /// </summary>
        /// <remarks>
        /// Returning dictionary contains the serialized properties of a state. You can obtain 
        /// the state data by using the <see cref="Storage.IStorageConnection.GetStateData"/>
        /// method. Please refer to documentation for this method in implementors to learn
        /// which key/value pairs are available.
        /// </remarks>
        /// <returns>A dictionary with serialized properties of the current state.</returns>
        [NotNull] Dictionary<string, string> SerializeData();
    }
}
