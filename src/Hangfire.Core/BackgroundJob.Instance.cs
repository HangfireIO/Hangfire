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

namespace Hangfire
{
    partial class BackgroundJob
    {
        /// <exclude />
        public BackgroundJob([NotNull] string id, [CanBeNull] Job job, DateTime createdAt)
            : this(id, job, createdAt, null)
        {
        }

        /// <exclude />
        public BackgroundJob([NotNull] string id, [CanBeNull] Job job, DateTime createdAt, [CanBeNull] IReadOnlyDictionary<string, string> parametersSnapshot)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            Id = id;
            Job = job;
            CreatedAt = createdAt;
            ParametersSnapshot = parametersSnapshot;
        }

        /// <exclude />
        [NotNull]
        public string Id { get; }

        /// <exclude />
        [CanBeNull]
        public Job Job { get; }

        /// <exclude />
        public DateTime CreatedAt { get; }

        /// <exclude />
        [CanBeNull]
        public IReadOnlyDictionary<string, string> ParametersSnapshot { get; }
    }
}
