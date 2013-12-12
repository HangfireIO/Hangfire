// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;

namespace HangFire.Server
{
    /// <summary>
    /// Provides information about the context in which the job
    /// is being performed.
    /// </summary>
    public class PerformContext : WorkerContext
    {
        internal PerformContext(PerformContext context)
            : this(context, context.JobDescriptor)
        {
            Items = context.Items;
        }

        internal PerformContext(
            WorkerContext workerContext, ServerJobDescriptorBase jobDescriptor)
            : base(workerContext)
        {
            JobDescriptor = jobDescriptor;
            Items = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets an instance of the key-value storage. You can use it
        /// to pass additional information between different client filters
        /// or just between different methods.
        /// </summary>
        public IDictionary<string, object> Items { get; private set; }

        /// <summary>
        /// Gets the client job descriptor that is associated with the
        /// current <see cref="PerformContext"/> object.
        /// </summary>
        public ServerJobDescriptorBase JobDescriptor { get; private set; }
    }
}
