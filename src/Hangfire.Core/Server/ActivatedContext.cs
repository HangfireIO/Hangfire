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
using Hangfire.Annotations;
using Hangfire.Storage;

namespace Hangfire.Server
{
    /// <summary>
    /// Provides the context for the <see cref="IActivationFilter.OnActivated"/>
    /// method of the <see cref="IActivationFilter"/> interface.
    /// </summary>
    public class ActivatedContext : ActivationContext
    {
        internal ActivatedContext(
            [NotNull] ActivationContext context,
            object instance,
            Exception exception)
            : this(context.Connection, context.BackgroundJob, instance, exception)
        { }

        private ActivatedContext(
            [NotNull] IStorageConnection connection,
            [NotNull] BackgroundJob backgroundJob,
            object instance,
            Exception exception)
            : base(connection, backgroundJob)
        {
            Instance = instance;
            Exception = exception;
        }

        /// <summary>
        /// Instance of the job
        /// </summary>
        public object Instance { get; }

        /// <summary>
        /// Exception that occured during creation of the job instance
        /// </summary>
        public Exception Exception { get; }
    }
}