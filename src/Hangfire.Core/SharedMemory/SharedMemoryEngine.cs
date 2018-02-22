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
    public abstract class SharedMemoryEngine : IDisposable
    {
        public abstract Task<SharedMemoryConnection> GetConnectionAsync(CancellationToken cancellationToken);
        public abstract Task<SharedMemoryReadOnlyConnection> GetReadOnlyConnection(CancellationToken cancellationToken);

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public virtual async Task<byte[][]> GetUniqueIdsAsync(int count, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = new byte[count][];

            for (var i = 0; i < count; i++)
            {
                result[i] = Guid.NewGuid().ToByteArray();
            }

            return result;
        }

        public virtual void Dispose()
        {
        }
    }
}