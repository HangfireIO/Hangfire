// This file is part of Hangfire. Copyright © 2019 Hangfire OÜ.
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
using System.Collections.Concurrent;
using System.Threading;

namespace Hangfire.Processing
{
    internal class InlineSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly ConcurrentQueue<Tuple<SendOrPostCallback, object>> _queue = new ConcurrentQueue<Tuple<SendOrPostCallback, object>>();
        private readonly Semaphore _semaphore = new Semaphore(0, Int32.MaxValue);

        public WaitHandle WaitHandle => _semaphore;

        public Tuple<SendOrPostCallback, object> Dequeue()
        {
            _queue.TryDequeue(out var tuple);
            return tuple;
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }

        public override void Post(SendOrPostCallback callback, object state)
        {
            try
            {
                _queue.Enqueue(Tuple.Create(callback, state));
                _semaphore.Release();
            }
            catch (ObjectDisposedException)
            {
                base.Post(callback, state);
            }
        }
    }
}