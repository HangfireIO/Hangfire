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
using System.Threading;
using Hangfire.Annotations;

namespace Hangfire.Server
{
    public class BackgroundProcessContext
    {
        public BackgroundProcessContext(
            [NotNull] string serverId,
            [NotNull] JobStorage storage, 
            CancellationToken cancellationToken)
        {
            if (serverId == null) throw new ArgumentNullException("serverId");
            if (storage == null) throw new ArgumentNullException("storage");

            Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            ServerId = serverId;
            Storage = storage;
            CancellationToken = cancellationToken;
        }

        public string ServerId { get; private set; }
        public IDictionary<string, object> Properties { get; private set; }

        public JobStorage Storage { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
    }
}