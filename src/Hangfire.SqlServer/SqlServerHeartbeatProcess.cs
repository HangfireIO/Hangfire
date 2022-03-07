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
using System.Collections.Concurrent;
using System.Threading;
using Hangfire.Common;
using Hangfire.Server;

namespace Hangfire.SqlServer
{
#pragma warning disable CS0618
    internal sealed class SqlServerHeartbeatProcess : IServerComponent
#pragma warning restore CS0618
    {
        private readonly ConcurrentDictionary<SqlServerTimeoutJob, object> _items =
            new ConcurrentDictionary<SqlServerTimeoutJob, object>();

        public void Track(SqlServerTimeoutJob item)
        {
            _items.TryAdd(item, null);
        }

        public void Untrack(SqlServerTimeoutJob item)
        {
            _items.TryRemove(item, out _);
        }

        public void Execute(CancellationToken cancellationToken)
        {
            foreach (var item in _items)
            {
                item.Key.ExecuteKeepAliveQueryIfRequired();
            }

            cancellationToken.Wait(TimeSpan.FromSeconds(1));
        }
    }
}