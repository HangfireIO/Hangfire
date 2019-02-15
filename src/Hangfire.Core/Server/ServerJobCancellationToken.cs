// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using Hangfire.Logging;

namespace Hangfire.Server
{
    internal class ServerJobCancellationToken : IJobCancellationToken, IDisposable
    {
        private static readonly ConcurrentDictionary<Guid, WeakReference<ServerJobCancellationToken>> WatchedTokens
            = new ConcurrentDictionary<Guid, WeakReference<ServerJobCancellationToken>>();
        
        private static readonly ILog Logger = LogProvider.For<ServerJobCancellationToken>();

        private class CancellationTokenHolder : IDisposable
        {
            private readonly CancellationTokenSource _abortedTokenSource;
            private readonly CancellationTokenSource _linkedTokenSource;

            public CancellationTokenHolder(bool aborted, CancellationToken shutdownToken)
            {
                _abortedTokenSource = new CancellationTokenSource();
                if (aborted)
                    _abortedTokenSource.Cancel();

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
        
        private readonly string _jobId;
        private readonly string _serverId;
        private readonly string _workerId;
        private readonly IStorageConnection _connection;
        private readonly CancellationToken _shutdownToken;
        private readonly Guid _uniqueId;
        private readonly Lazy<CancellationTokenHolder> _cancellationTokenHolder;
        private volatile bool _isAborted;

        public ServerJobCancellationToken(
            [NotNull] IStorageConnection connection,
            [NotNull] string jobId, 
            [NotNull] string serverId,
            [NotNull] string workerId,
            CancellationToken shutdownToken)
        {
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));
            if (serverId == null) throw new ArgumentNullException(nameof(serverId));
            if (workerId == null) throw new ArgumentNullException(nameof(workerId));
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            _jobId = jobId;
            _serverId = serverId;
            _workerId = workerId;
            _connection = connection;
            _shutdownToken = shutdownToken;

            _uniqueId = Guid.NewGuid();

            _cancellationTokenHolder = new Lazy<CancellationTokenHolder>(
                () => new CancellationTokenHolder(_isAborted, _shutdownToken),
                LazyThreadSafetyMode.ExecutionAndPublication);

            _isAborted = false;
            WatchedTokens.TryAdd(_uniqueId, new WeakReference<ServerJobCancellationToken>(this));
        }

        ~ServerJobCancellationToken()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            StopAbortChecks();

            if (disposing && _cancellationTokenHolder.IsValueCreated)
            {
                _cancellationTokenHolder.Value.Dispose();
            }
        }

        public CancellationToken ShutdownToken => _cancellationTokenHolder.Value.CancellationToken;

        public bool IsAborted => _cancellationTokenHolder.IsValueCreated ? _cancellationTokenHolder.Value.IsAborted : _isAborted;

        private void StopAbortChecks()
        {
            WeakReference<ServerJobCancellationToken> _;
            WatchedTokens.TryRemove(_uniqueId, out _);
        }

        internal void Abort()
        {
            _isAborted = true;
            
            if (_cancellationTokenHolder.IsValueCreated)
            {
                _cancellationTokenHolder.Value.Abort();
            }

            // remove aborted token from WatchedTokens collection
            // to prevent further checks, since it is already aborted
            StopAbortChecks();
        }

        public void ThrowIfCancellationRequested()
        {
            _shutdownToken.ThrowIfCancellationRequested();

            if (IsAborted)
            {
                throw new JobAbortedException();
            }
            
            if (IsJobAborted(_connection))
            {
                Abort();
                throw new JobAbortedException();
            }
        }

        private bool IsJobAborted(IStorageConnection connection)
        {
            var state = connection.GetStateData(_jobId);

            if (state == null)
            {
                return true;
            }

            if (!state.Name.Equals(ProcessingState.StateName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!state.Data.ContainsKey("ServerId"))
            {
                return true;
            }

            if (!state.Data["ServerId"].Equals(_serverId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!state.Data.ContainsKey("WorkerId"))
            {
                return true;
            }

            if (!state.Data["WorkerId"].Equals(_workerId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static void CheckAllCancellationTokens(IStorageConnection connection)
        {
            foreach (var tokenRef in WatchedTokens.Values)
            {
                ServerJobCancellationToken token;
                if (tokenRef.TryGetTarget(out token) && token.IsJobAborted(connection))
                {
                    Logger.Debug($"Job {token._jobId} will be aborted");
                    token.Abort();
                }
            }
        }

    }
}