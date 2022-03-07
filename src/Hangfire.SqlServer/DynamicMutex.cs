// This file is part of Hangfire. Copyright © 2021 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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