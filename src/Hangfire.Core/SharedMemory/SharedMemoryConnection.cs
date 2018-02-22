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
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.SharedMemory
{
    public abstract class SharedMemoryConnection : IDisposable
    {
        protected SharedMemoryConnection(SharedMemoryEngine engine)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public SharedMemoryEngine Engine { get; }

        public abstract Task SessionCreateAsync(Guid sessionId, string type, byte[] data, TimeSpan timeout, TimeSpan deadline, CancellationToken cancellationToken);
        public abstract Task<bool> SessionTryUpdateAsync(Guid sessionId, string type, byte[] data, CancellationToken cancellationToken);
        public abstract Task<bool> SessionTryDeleteAsync(Guid sessionId, TimeSpan delay, CancellationToken cancellationToken);

        public abstract Task<bool> QueueTryCreateAsync(string queueId, int flags, CancellationToken cancellationToken);
        public abstract Task<bool> QueueTryUpdateAsync(string queueId, int flags, CancellationToken cancellationToken);
        public abstract Task<bool> QueueTryDeleteAsync(string queueId, CancellationToken cancellationToken);

        public abstract Task<bool> StreamTryCreateAsync(string streamId, TimeSpan retentionTime, CancellationToken cancellationToken);
        public abstract Task<bool> StreamTryUpdateAsync(string streamId, TimeSpan retentionTime, CancellationToken cancellationToken);
        public abstract Task<bool> StreamTryDeleteAsync(string streamId, CancellationToken cancellationToken);

        public abstract Task<DateTime> GetUtcTimeAsync(CancellationToken cancellationToken);

        public abstract Task<ISharedMemoryMessage[]> MessageDequeueAsync(string queueId, Guid sessionId, int count, CancellationToken cancellationToken);
        public abstract Task<ISharedMemoryEvent[]> EventReadAsync(string streamId, byte[] key, byte[] offset, int count, CancellationToken cancellationToken);

        public abstract Task<SharedMemoryTransaction> GetTransactionAsync(CancellationToken cancellationToken);

        public virtual void Dispose()
        {
        }
    }
}