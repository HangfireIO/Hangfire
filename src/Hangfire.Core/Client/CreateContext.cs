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
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.Client
{
    /// <summary>
    /// Provides information about the context in which the job is created.
    /// </summary>
    public class CreateContext
    {
        public CreateContext([NotNull] CreateContext context)
            : this(context.Storage, context.Connection, context.Job, context.InitialState)
        {
            Items = context.Items;
            Parameters = context.Parameters;
        }

        public CreateContext(
            [NotNull] JobStorage storage, 
            [NotNull] IStorageConnection connection, 
            [NotNull] Job job, 
            [CanBeNull] IState initialState)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (job == null) throw new ArgumentNullException(nameof(job));

            Storage = storage;
            Connection = connection;
            Job = job;
            InitialState = initialState;

            Items = new Dictionary<string, object>();
            Parameters = new Dictionary<string, object>();
        }

        [NotNull]
        public JobStorage Storage { get; }

        [NotNull]
        public IStorageConnection Connection { get; }

        /// <summary>
        /// Gets an instance of the key-value storage. You can use it
        /// to pass additional information between different client filters
        /// or just between different methods.
        /// </summary>
        [NotNull]
        public IDictionary<string, object> Items { get; }

        [NotNull]
        public virtual IDictionary<string, object> Parameters { get; }
            
        [NotNull]
        public Job Job { get; }

        /// <summary>
        /// Gets the initial state of the creating job. Note, that
        /// the final state of the created job could be changed after 
        /// the registered instances of the <see cref="IElectStateFilter"/>
        /// class are doing their job.
        /// </summary>
        [CanBeNull]
        public IState InitialState { get; }
    }
}