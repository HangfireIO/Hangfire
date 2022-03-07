// This file is part of Hangfire. Copyright © 2018 Sergey Odinokov.
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

namespace Hangfire.Server
{
    public class BackgroundServerContext
    {
        public BackgroundServerContext(
            [NotNull] string serverId,
            [NotNull] JobStorage storage,
            [NotNull] IDictionary<string, object> properties, 
            CancellationToken stoppingToken, 
            CancellationToken stoppedToken,
            CancellationToken shutdownToken)
        {
            ServerId = serverId ?? throw new ArgumentNullException(nameof(serverId));
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            StoppingToken = stoppingToken;
            StoppedToken = stoppedToken;
            ShutdownToken = shutdownToken;
        }

        public string ServerId { get; }
        public JobStorage Storage { get; }
        public IDictionary<string, object> Properties { get; }
        public CancellationToken StoppingToken { get; }
        public CancellationToken StoppedToken { get; }
        public CancellationToken ShutdownToken { get; }
    }
}
