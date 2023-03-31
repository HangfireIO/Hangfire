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
using Newtonsoft.Json;

namespace Hangfire.States
{
    /// <summary>
    /// Defines the <i>intermediate</i> state of a background job when a 
    /// <see cref="Hangfire.Server.Worker"/> has started to process it.
    /// </summary>
    /// 
    /// <threadsafety static="true" instance="false"/>
    public class ProcessingState : IState
    {
        /// <summary>
        /// Represents the name of the <i>Processing</i> state. This field is read-only.
        /// </summary>
        /// <remarks>
        /// The value of this field is <c>"Processing"</c>.
        /// </remarks>
        public static readonly string StateName = "Processing";

        internal ProcessingState(string serverId, string workerId)
        {
            if (String.IsNullOrWhiteSpace(serverId)) throw new ArgumentNullException(nameof(serverId));

            ServerId = serverId;
            StartedAt = DateTime.UtcNow;
            WorkerId = workerId;
        }

        /// <summary>
        /// Gets a date/time when the current state instance was created.
        /// </summary>
        [JsonIgnore]
        public DateTime StartedAt { get; }

        /// <summary>
        /// Gets the <i>instance id</i> of an instance of the <see cref="BackgroundProcessingServer"/>
        /// class, whose <see cref="Server.Worker"/> background process started to process an 
        /// <i>enqueued</i> background job.
        /// </summary>
        /// <value>Usually the string representation of a GUID value, may vary in future versions.</value>
        public string ServerId { get; }

        /// <summary>
        /// Gets the identifier of a <see cref="Server.Worker"/> that started to
        /// process an <i>enqueued</i> background job.
        /// </summary>
        public string WorkerId { get; }

        /// <inheritdoc />
        /// <remarks>
        /// Always equals to <see cref="StateName"/> for the <see cref="ProcessingState"/>.
        /// Please see the remarks section of the <see cref="IState.Name">IState.Name</see>
        /// article for the details.
        /// </remarks>
        [JsonIgnore]
        public string Name => StateName;

        /// <inheritdoc />
        public string Reason { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="true"/> for the <see cref="ProcessingState"/>.
        /// Please refer to the <see cref="IState.IsFinal">IState.IsFinal</see> documentation
        /// for the details.
        /// </remarks>
        [JsonIgnore]
        public bool IsFinal => false;

        /// <inheritdoc />
        /// <remarks>
        /// Always returns <see langword="false" /> for the <see cref="ProcessingState"/>.
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
        ///         <term><c>StartedAt</c></term>
        ///         <term><see cref="DateTime"/></term>
        ///         <term><see cref="JobHelper.DeserializeDateTime"/></term>
        ///         <description>Please see the <see cref="StartedAt"/> property.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>ServerId</c></term>
        ///         <term><see cref="string"/></term>
        ///         <term><i>Not required</i></term>
        ///         <description>Please see the <see cref="ServerId"/> property.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>WorkerId</c></term>
        ///         <term><see cref="string"/></term>
        ///         <term><i>Not required</i></term>
        ///         <description>Please see the <see cref="WorkerId"/> property.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "StartedAt", JobHelper.SerializeDateTime(StartedAt) },
                { "ServerId", ServerId },
                { "WorkerId", WorkerId }
            };
        }
    }
}
