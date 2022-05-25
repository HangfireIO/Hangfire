// This file is part of Hangfire. Copyright © 2021 Hangfire OÜ.
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

namespace Hangfire.SqlServer
{
    internal sealed class DynamicMutex<T>
    {
        private readonly HashSet<T> _acquiredResources = new HashSet<T>();

        public void Wait([CanBeNull] T resource, CancellationToken cancellationToken, out bool acquired)
        {
            using (cancellationToken.Register(PulseAllWhenCanceled))
            {
                lock (_acquiredResources)
                {
                    acquired = false;

                    while (_acquiredResources.Contains(resource) && !cancellationToken.IsCancellationRequested)
                    {
                        Monitor.Wait(_acquiredResources);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    _acquiredResources.Add(resource);
                    acquired = true;
                }
            }
        }

        public void Release([CanBeNull] T resource)
        {
            lock (_acquiredResources)
            {
                if (!_acquiredResources.Remove(resource))
                {
                    throw new InvalidOperationException($"Resource '{resource}' isn't acquired and can't be released.");
                }

                Monitor.Pulse(_acquiredResources);
            }
        }

        private void PulseAllWhenCanceled()
        {
            lock (_acquiredResources)
            {
                Monitor.PulseAll(_acquiredResources);
            }
        }
    }
}