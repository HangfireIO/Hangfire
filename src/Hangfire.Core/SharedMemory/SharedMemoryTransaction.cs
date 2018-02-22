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
    public abstract class SharedMemoryTransaction : IDisposable
    {
        protected SharedMemoryTransaction(SharedMemoryConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            Connection = connection;
        }

        public SharedMemoryConnection Connection { get; }

        public abstract Task<ISharedMemoryBlock[]> BlockGetAsync(byte[][] blockIds, CancellationToken cancellationToken);

        public abstract void BlockCreate(byte[] blockId, string type, byte[] data, byte[] categoryId, TimeSpan? expireIn = null);
        public abstract void BlockUpdate(ISharedMemoryBlock block, string type, byte[] data, byte[] categoryId, TimeSpan? expireIn = null);

        public abstract void EventAdd(string streamId, byte[] key, string type, byte[] payload);

        public abstract void MessageAdd(string queueId, string type, byte[] payload, TimeSpan delay, DateTime? at = null, Guid? sessionId = null);
        public abstract void MessageRemove(ISharedMemoryMessage message);

        public abstract Task FlushAsync(CancellationToken cancellationToken);

        public virtual void Dispose()
        {
        }
    }
}