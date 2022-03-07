// This file is part of Hangfire. Copyright © 2019 Sergey Odinokov.
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