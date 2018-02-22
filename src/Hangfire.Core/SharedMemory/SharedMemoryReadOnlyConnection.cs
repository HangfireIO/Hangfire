// This file is part of Hangfire.
// Copyright © 2018 Sergey Odinokov.
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
using System.Threading.Tasks;

namespace Hangfire.SharedMemory
{
    public abstract class SharedMemoryReadOnlyConnection : IDisposable
    {
        protected SharedMemoryReadOnlyConnection(SharedMemoryEngine engine)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public SharedMemoryEngine Engine { get; }

        // Blocks
        public abstract Task<IReadOnlyCollection<ISharedMemoryBlock>> BlockGetAsync(byte[][] blockIds);

        public abstract Task<IReadOnlyCollection<string>> BlockGetByCategoryAsync(byte[][] categoryIds, long from, int count, bool ascending);
        public abstract Task<long> BlockGetCountByCategoryAsync(byte[][] categoryIds, long limit);

        public abstract Task<IReadOnlyCollection<string>> BlockGetExpiringByCategoryAsync(byte[][] categoryIds, long from, int count, bool ascending);
        public abstract Task<long> BlockGetExpiringCountByCategoryAsync(byte[][] categoryIds, long limit);

        // Events
        public abstract Task<IReadOnlyCollection<ISharedMemoryEvent>> EventGetAsync(string streamId, Tuple<byte[], byte[]>[] keyOffsets, int count, bool ascending);

        // Messages
        public abstract Task<IReadOnlyCollection<ISharedMemoryMessage>> MessageGetAsync(string[] queueIds, int count);
        public abstract Task<long> MessageGetCountAsync(string[] queueIds, long limit);

        public abstract Task<IReadOnlyCollection<ISharedMemoryMessage>> MessageGetDelayedAsync(string[] queueIds, int count);
        public abstract Task<long> MessageGetDelayedCountAsync(string[] queueIds, long limit);

        public abstract Task<IReadOnlyCollection<ISharedMemoryMessage>> MessageGetBySessionAsync(string sessionId, string[] queueIds);
        public abstract Task<IReadOnlyCollection<ISharedMemoryMessage>> MessageGetBySessionCountAsync(string sessionId, string[] queueIds, long limit);

        // Queues
        public abstract Task<IReadOnlyCollection<ISharedMemoryQueue>> QueueGetRangeAsync(long from, int count, bool ascending);
        public abstract Task<long> QueueGetCountAsync(long limit);

        // Streams
        public abstract Task<IReadOnlyCollection<ISharedMemoryStream>> StreamGetRangeAsync(long from, int count, bool ascending);
        public abstract Task<long> StreamGetCountAsync(long limit);

        // Session
        public abstract Task<IReadOnlyCollection<ISharedMemorySession>> SessionGetAsync(Guid[] sessionIds);
        public abstract Task<IReadOnlyCollection<ISharedMemorySession>> SessionGetRangeAsync(long from, int count, bool ascending);
        public abstract Task<long> SessionGetCountAsync(long limit);

        public virtual void Dispose()
        {
        }
    }
}
