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
            [NotNull] IDictionary<string, object> properties, 
            CancellationToken cancellationToken)
        {
            if (serverId == null) throw new ArgumentNullException("serverId");
            if (storage == null) throw new ArgumentNullException("storage");
            if (properties == null) throw new ArgumentNullException("properties");

            ServerId = serverId;
            Storage = storage;
            Properties = new Dictionary<string, object>(properties, StringComparer.OrdinalIgnoreCase);
            CancellationToken = cancellationToken;
        }
        
        [NotNull]
        public string ServerId { get; private set; }

        [NotNull]
        public IReadOnlyDictionary<string, object> Properties { get; private set; }

        [NotNull]
        public JobStorage Storage { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public bool IsShutdownRequested
        {
            get { return CancellationToken.IsCancellationRequested; }
        }

        public void Wait(TimeSpan timeout)
        {
            CancellationToken.WaitHandle.WaitOne(timeout);
        }
    }
}