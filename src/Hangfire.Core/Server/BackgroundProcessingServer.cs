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
using System.Collections.Generic;
#if !NETSTANDARD1_3
using System.Diagnostics;
#endif
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.Processing;

namespace Hangfire.Server
{
    /// <summary>
    /// Responsible for running the given collection background processes.
    /// </summary>
    /// 
    /// <remarks>
    /// Immediately starts the processes in a background thread.
    /// Responsible for announcing/removing a server, bound to a storage.
    /// Wraps all the processes with a infinite loop and automatic retry.
    /// Executes all the processes in a single context.
    /// Uses timeout in dispose method, waits for all the components, cancel signals shutdown
    /// Contains some required processes and uses storage processes.
    /// Generates unique id.
    /// Properties are still bad.
    /// </remarks>
    public sealed class BackgroundProcessingServer : IBackgroundProcessingServer
    {
        private static int _lastThreadId = 0;

        private readonly ILog _logger = LogProvider.GetLogger(typeof(BackgroundProcessingServer));
        private readonly CancellationTokenSource _stopCts = new CancellationTokenSource();
        private readonly CancellationTokenSource _abortCts = new CancellationTokenSource();

        private readonly IBackgroundServerProcess _process;
        private readonly BackgroundProcessingServerOptions _options;
        private readonly IBackgroundDispatcher _dispatcher;

        private int _shutdown;

        public BackgroundProcessingServer([NotNull] IEnumerable<IBackgroundProcess> processes)
            : this(JobStorage.Current, processes)
        {
        }

        public BackgroundProcessingServer(
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> properties)
            : this(JobStorage.Current, processes, properties)
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> processes)
            : this(storage, processes, new Dictionary<string, object>())
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> properties)
            : this(storage, processes, properties, new BackgroundProcessingServerOptions())
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcess> processes,
            [NotNull] IDictionary<string, object> properties,
            [NotNull] BackgroundProcessingServerOptions options)
            : this(storage, GetProcesses(processes), properties, options)
        {
        }

        public BackgroundProcessingServer(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcessDispatcherBuilder> dispatcherBuilders,
            [NotNull] IDictionary<string, object> properties,
            [NotNull] BackgroundProcessingServerOptions options)
            : this(new BackgroundServerProcess(storage, dispatcherBuilders, options, properties), options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundProcessingServer"/>
        /// class and immediately starts all the given background processes.
        /// </summary>
        internal BackgroundProcessingServer(
            [NotNull] BackgroundServerProcess process,
            [NotNull] BackgroundProcessingServerOptions options)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _process = process;
            _options = options;

            _dispatcher = CreateDispatcher();

#if !NETSTANDARD1_3
            AppDomain.CurrentDomain.DomainUnload += OnCurrentDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += OnCurrentDomainUnload;
#endif
        }

        public void Stop(bool abort)
        {
            _stopCts.Cancel();
            if (abort)
            {
                _abortCts.Cancel();
            }
        }

        public bool Wait(TimeSpan timeout)
        {
            ThrowIfDisposed();
            return _dispatcher.Wait(timeout);
        }

        public Task WaitAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _dispatcher.WaitAsync(cancellationToken);
        }

        [Obsolete("This method is deprecated, please use the Stop(bool) method instead. Will be removed in 2.0.0.")]
        public void SendStop()
        {
            Stop(false);
        }

        public void Dispose()
        {
            Shutdown(_options.ShutdownTimeout);

            _dispatcher.Dispose();
            _abortCts.Dispose();
            _stopCts.Dispose();
        }

        private void OnCurrentDomainUnload(object sender, EventArgs args)
        {
            Shutdown(_options.ForcedShutdownTimeout);
        }

        private void Shutdown(TimeSpan timeout)
        {
            if (Interlocked.Exchange(ref _shutdown, 1) == 1)
            {
                return;
            }

            if (!_stopCts.IsCancellationRequested)
            {
                _logger.Info($"Shutdown initiated with the {timeout} timeout...");

                _stopCts.Cancel();

                if (!_dispatcher.Wait(timeout))
                {
                    _abortCts.Cancel();
                    _dispatcher.Wait(_options.AbortTimeout);
                }
            }
        }

        private static IBackgroundProcessDispatcherBuilder[] GetProcesses([NotNull] IEnumerable<IBackgroundProcess> processes)
        {
            if (processes == null) throw new ArgumentNullException(nameof(processes));
            return processes.Select(x => x.UseBackgroundPool(threadCount: 1)).ToArray();
        }

        private IBackgroundDispatcher CreateDispatcher()
        {
            var execution = new BackgroundExecution(
                _stopCts.Token,
                _abortCts.Token,
                new BackgroundExecutionOptions
                {
                    Name = nameof(BackgroundServerProcess),
                    ErrorThreshold = TimeSpan.Zero,
                    StillErrorThreshold = TimeSpan.Zero,
                    RetryDelay = retry => _options.RestartDelay
                });

            return new BackgroundDispatcher(
                execution,
                RunServer,
                execution,
                ThreadFactory);
        }

        private void RunServer(Guid executionId, object state)
        {
            _process.Execute(executionId, (BackgroundExecution)state, _stopCts.Token, _abortCts.Token);
        }

        private static IEnumerable<Thread> ThreadFactory(ThreadStart threadStart)
        {
            yield return new Thread(threadStart)
            {
                IsBackground = true,
                Name = $"{nameof(BackgroundServerProcess)} #{Interlocked.Increment(ref _lastThreadId)}",
#if !NETSTANDARD1_3
                Priority = ThreadPriority.AboveNormal
#endif
            };
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _shutdown) == 1)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
