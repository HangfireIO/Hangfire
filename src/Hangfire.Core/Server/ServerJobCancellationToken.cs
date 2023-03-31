// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using Hangfire.Annotations;
using Hangfire.States;
using Hangfire.Storage;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Hangfire.Server
{
    internal class ServerJobCancellationToken : IJobCancellationToken, IDisposable
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<ServerJobCancellationToken, object>> WatchedServers
            = new ConcurrentDictionary<string, ConcurrentDictionary<ServerJobCancellationToken, object>>();

        private readonly object _syncRoot = new object();
        private readonly string _jobId;
        private readonly string _serverId;
        private readonly string _workerId;
        private readonly IStorageConnection _connection;
        private readonly CancellationToken _shutdownToken;
        private readonly Lazy<CancellationTokenHolder> _cancellationTokenHolder;
        private readonly ConcurrentDictionary<ServerJobCancellationToken, object> _watchedTokens;
        private bool _disposed;

        public ServerJobCancellationToken(
            [NotNull] IStorageConnection connection,
            [NotNull] string jobId, 
            [NotNull] string serverId,
            [NotNull] string workerId,
            CancellationToken shutdownToken)
        {
            _jobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
            _serverId = serverId ?? throw new ArgumentNullException(nameof(serverId));
            _workerId = workerId ?? throw new ArgumentNullException(nameof(workerId));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            _shutdownToken = shutdownToken;

            _cancellationTokenHolder = new Lazy<CancellationTokenHolder>(
                () => new CancellationTokenHolder(_shutdownToken),
                LazyThreadSafetyMode.None);

            if (WatchedServers.TryGetValue(_serverId, out _watchedTokens))
            {
                _watchedTokens.TryAdd(this, null);
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed) return;
                _disposed = true;

                _watchedTokens?.TryRemove(this, out _);

                if (_cancellationTokenHolder.IsValueCreated)
                {
                    _cancellationTokenHolder.Value.Dispose();
                }
            }
        }

        public CancellationToken ShutdownToken
        {
            get
            {
                lock (_syncRoot)
                {
                    CheckDisposed();
                    return _cancellationTokenHolder.Value.CancellationToken;
                }
            }
        }

        public bool IsAborted
        {
            get
            {
                lock (_syncRoot)
                {
                    CheckDisposed();
                    return _cancellationTokenHolder.IsValueCreated && _cancellationTokenHolder.Value.IsAborted;
                }
            }
        }

        public void ThrowIfCancellationRequested()
        {
            lock (_syncRoot)
            {
                CheckDisposed();

                _shutdownToken.ThrowIfCancellationRequested();

                if (_cancellationTokenHolder.IsValueCreated && _cancellationTokenHolder.Value.IsAborted)
                {
                    throw new JobAbortedException();
                }

                // TODO: Create a new connection instead to avoid possible race conditions due to user code
                if (CheckJobStateChanged(_connection))
                {
                    throw new JobAbortedException();
                }
            }
        }

        public static void AddServer(string serverId)
        {
            WatchedServers.TryAdd(serverId, new ConcurrentDictionary<ServerJobCancellationToken, object>());
        }

        public static void RemoveServer(string serverId)
        {
            WatchedServers.TryRemove(serverId, out _);
        }

        public static IEnumerable<Tuple<string, string>> CheckAllCancellationTokens(
            string serverId,
            IStorageConnection connection,
            CancellationToken cancellationToken)
        {
            if (WatchedServers.TryGetValue(serverId, out var watchedTokens))
            {
                var result = new List<Tuple<string, string>>();

                foreach (var token in watchedTokens)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (token.Key.TryCheckJobIsAborted(connection))
                    {
                        result.Add(Tuple.Create(token.Key._jobId, token.Key._workerId));
                    }
                }

                return result;
            }

            return Enumerable.Empty<Tuple<string, string>>();
        }

        public bool TryCheckJobIsAborted(IStorageConnection connection)
        {
            // Returns `true` only when check was performed AND the job is
            // aborted AND it was not already aborted by calling this method.
            // When return value is `false`, this means either the check
            // wasn't performed (because object is disposed, or there's no
            // associated token holder) or job is still running.

            lock (_syncRoot)
            {
                if (_disposed || !_cancellationTokenHolder.IsValueCreated || _cancellationTokenHolder.Value.IsAborted)
                {
                    return false;
                }

                return CheckJobStateChanged(connection);
            }
        }

        private bool CheckJobStateChanged(IStorageConnection connection)
        {
            if (IsJobStateChanged(connection))
            {
                _cancellationTokenHolder.Value.Abort();
                return true;
            }

            return false;
        }

        private bool IsJobStateChanged(IStorageConnection connection)
        {
            var state = connection.GetStateData(_jobId);

            if (state == null || !state.Name.Equals(ProcessingState.StateName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!state.Data.ContainsKey("ServerId") || !state.Data["ServerId"].Equals(_serverId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!state.Data.ContainsKey("WorkerId") || !state.Data["WorkerId"].Equals(_workerId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private class CancellationTokenHolder : IDisposable
        {
            private readonly CancellationTokenSource _abortedTokenSource;
            private readonly CancellationTokenSource _linkedTokenSource;

            public CancellationTokenHolder(CancellationToken shutdownToken)
            {
                _abortedTokenSource = new CancellationTokenSource();
                _linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken, _abortedTokenSource.Token);
            }

            public CancellationToken CancellationToken => _linkedTokenSource.Token;

            public bool IsAborted => _abortedTokenSource.IsCancellationRequested;

            public void Abort()
            {
                _abortedTokenSource.Cancel();
            }

            public void Dispose()
            {
                _linkedTokenSource.Dispose();
                _abortedTokenSource.Dispose();
            }
        }
    }
}