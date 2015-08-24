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
using Hangfire.Common;
using Hangfire.Storage;

namespace Hangfire.States
{
    /// <exclude />
    public class StateMachineFactory : IStateMachineFactory
    {
        private readonly JobStorage _storage;
        private readonly IJobFilterProvider _filterProvider;

        public StateMachineFactory([NotNull] JobStorage storage)
            : this(storage, JobFilterProviders.Providers)
        {
        }

        public StateMachineFactory([NotNull] JobStorage storage, [NotNull] IJobFilterProvider filterProvider)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (filterProvider == null) throw new ArgumentNullException("filterProvider");
            
            _storage = storage;
            _filterProvider = filterProvider;
        }

        public IStateMachine Create(IStorageConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            var process = new DefaultStateChangeProcess(_filterProvider);
            return new StateMachine(process);
        }
    }
}
