// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
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
using Hangfire.Common;

namespace Hangfire.Server
{
    public class BackgroundProcessContext
    {
        [Obsolete("This constructor overload is deprecated and will be removed in 2.0.0.")]
        public BackgroundProcessContext(
            [NotNull] string serverId,
            [NotNull] JobStorage storage,
            [NotNull] IDictionary<string, object> properties,
            CancellationToken cancellationToken)
            : this(serverId, storage, properties, Guid.NewGuid(), cancellationToken, cancellationToken, cancellationToken)
        {
        }

        public BackgroundProcessContext(
            [NotNull] string serverId,
            [NotNull] JobStorage storage, 
            [NotNull] IDictionary<string, object> properties, 
            Guid executionId,
            CancellationToken stoppingToken,
            CancellationToken stoppedToken,
            CancellationToken shutdownToken)
        {
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            ServerId = serverId;
            Storage = storage;
            ExecutionId = executionId;
            Properties = new Dictionary<string, object>(properties, StringComparer.OrdinalIgnoreCase);
            StoppingToken = stoppingToken;
            StoppedToken = stoppedToken;
            ShutdownToken = shutdownToken;
        }
        
        [NotNull]
        public string ServerId { get; }

        [NotNull]
        public IReadOnlyDictionary<string, object> Properties { get; }

        [NotNull]
        public JobStorage Storage { get; }

        public Guid ExecutionId { get; }

        [Obsolete("Please use the StoppingToken property instead, will be removed in 2.0.0.")]
        public CancellationToken CancellationToken => StoppingToken;

        public CancellationToken StoppingToken { get; }
        public CancellationToken StoppedToken { get; }
        public CancellationToken ShutdownToken { get; }

        public bool IsStopping => StoppingToken.IsCancellationRequested || StoppedToken.IsCancellationRequested || ShutdownToken.IsCancellationRequested;
        public bool IsStopped => StoppedToken.IsCancellationRequested || ShutdownToken.IsCancellationRequested;

        [Obsolete("Please use IsStopping or IsStopped properties instead. Will be removed in 2.0.0.")]
        public bool IsShutdownRequested => StoppingToken.IsCancellationRequested;

        public void Wait(TimeSpan timeout)
        {
            StoppingToken.Wait(timeout);
        }
    }
}