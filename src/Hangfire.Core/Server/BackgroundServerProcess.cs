﻿// This file is part of Hangfire. Copyright © 2017 Hangfire OÜ.
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.Processing;

namespace Hangfire.Server
{
    internal sealed class BackgroundServerProcess : IBackgroundServerProcess
    {
        private static readonly char[] ColonSeparator = new [] { ':' };

        private readonly ILog _logger = LogProvider.GetLogger(typeof(BackgroundServerProcess));
        private readonly JobStorage _storage;
        private readonly BackgroundProcessingServerOptions _options;
        private readonly IDictionary<string, object> _properties;
        private readonly IBackgroundProcessDispatcherBuilder[] _dispatcherBuilders;

        public BackgroundServerProcess(
            [NotNull] JobStorage storage,
            [NotNull] IEnumerable<IBackgroundProcessDispatcherBuilder> dispatcherBuilders,
            [NotNull] BackgroundProcessingServerOptions options,
            [NotNull] IDictionary<string, object> properties)
        {
            if (dispatcherBuilders == null) throw new ArgumentNullException(nameof(dispatcherBuilders));

            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _properties = properties ?? throw new ArgumentNullException(nameof(properties));

            var builders = new List<IBackgroundProcessDispatcherBuilder>();
            builders.AddRange(GetRequiredProcesses());
            if (!options.ExcludeStorageProcesses)
            {
                builders.AddRange(GetStorageComponents());
            }
            builders.AddRange(dispatcherBuilders);

            _dispatcherBuilders = builders.ToArray();
        }

        public void Execute(Guid executionId, BackgroundExecution execution, CancellationToken stoppingToken,
            CancellationToken stoppedToken, CancellationToken shutdownToken)
        {
            var serverId = GetServerId();
            Stopwatch stoppedAt = null;

            void HandleStopRestartSignal() => Interlocked.CompareExchange(ref stoppedAt, Stopwatch.StartNew(), null);
            void HandleStoppingSignal() => _logger.Info($"{GetServerTemplate(serverId)} caught stopping signal...");
            void HandleStoppedSignal() => _logger.Info($"{GetServerTemplate(serverId)} caught stopped signal...");
            void HandleShutdownSignal() => _logger.Warn($"{GetServerTemplate(serverId)} caught shutdown signal...");

            void HandleRestartSignal()
            {
                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.Info($"{GetServerTemplate(serverId)} caught restart signal...");
                }
            }

            //using (LogProvider.OpenMappedContext("ServerId", serverId.ToString()))
            using (var restartCts = new CancellationTokenSource())
            using (var restartStoppingCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, restartCts.Token))
            using (var restartStoppedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppedToken, restartCts.Token))
            using (var restartShutdownCts = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken, restartCts.Token))
            using (restartStoppingCts.Token.Register(HandleStopRestartSignal))
            using (stoppingToken.Register(HandleStoppingSignal))
            using (stoppedToken.Register(HandleStoppedSignal))
            using (shutdownToken.Register(HandleShutdownSignal))
            using (restartCts.Token.Register(HandleRestartSignal))
            {
                var context = new BackgroundServerContext(
                    serverId,
                    _storage,
                    _properties,
                    restartStoppingCts.Token,
                    restartStoppedCts.Token,
                    restartShutdownCts.Token);

                var dispatchers = new List<IBackgroundDispatcher>();

                CreateServer(context);

                try
                {
                    // ReSharper disable once AccessToDisposedClosure
                    using (var heartbeat = CreateHeartbeatProcess(context, () => restartCts.Cancel()))
                    {
                        StartDispatchers(context, dispatchers);
                        execution.NotifySucceeded();

                        WaitForDispatchers(context, dispatchers);

                        restartCts.Cancel();

                        // TODO Either modify the IBackgroundDispatcher.Wait method to handle CancellationToken
                        // or expose the WaitHandle property to not to perform sync-over-async and vice versa
                        // in 2.0.
                        heartbeat.WaitAsync(Timeout.InfiniteTimeSpan, shutdownToken).GetAwaiter().GetResult();
                    }
                }
                finally
                {
                    DisposeDispatchers(dispatchers);
                    ServerDelete(context, stoppedAt);
                }
            }
        }

        private IBackgroundDispatcher CreateHeartbeatProcess(BackgroundServerContext context, Action requestRestart)
        {
            // We need to ensure that heartbeats are sent until server shutdown. Otherwise our server
            // can be considered as dead before completing all the background jobs on a graceful
            // shutdown.
            var heartbeatContext = new BackgroundServerContext(
                context.ServerId,
                context.Storage,
                context.Properties,
                context.ShutdownToken,
                context.ShutdownToken,
                context.ShutdownToken);

            return new ServerHeartbeatProcess(_options.HeartbeatInterval, _options.ServerTimeout, requestRestart)
                .UseBackgroundPool(threadCount: 1
#if !NETSTANDARD1_3
                    , static thread => { thread.Priority = ThreadPriority.AboveNormal; }
#endif
                )
                .Create(heartbeatContext, _options);
        }

        private IEnumerable<IBackgroundProcessDispatcherBuilder> GetRequiredProcesses()
        {
            yield return new ServerWatchdog(_options.ServerCheckInterval, _options.ServerTimeout).UseBackgroundPool(threadCount: 1);
            yield return new ServerJobCancellationWatcher(_options.CancellationCheckInterval).UseBackgroundPool(threadCount: 1);

            if (_storage.HasFeature(Storage.JobStorageFeatures.ProcessesInsteadOfComponents))
            {
                foreach (var serverProcess in _storage.GetServerRequiredProcesses())
                {
                    yield return serverProcess.UseBackgroundPool(threadCount: 1);
                }
            }
        }

        private IEnumerable<IBackgroundProcessDispatcherBuilder> GetStorageComponents()
        {
            if (_storage.HasFeature(Storage.JobStorageFeatures.ProcessesInsteadOfComponents))
            {
                return _storage.GetStorageWideProcesses().Select(static process => process.UseBackgroundPool(threadCount: 1));
            }

#pragma warning disable CS0618 // Type or member is obsolete
            return _storage.GetComponents().Select(static component => new ServerProcessDispatcherBuilder(
#pragma warning restore CS0618 // Type or member is obsolete
                component, 
                threadStart => BackgroundProcessExtensions.DefaultThreadFactory(1, component.GetType().Name, threadStart, null)));
        }

        private string GetServerId()
        {
            var serverName = _options.ServerName
                 ?? Environment.GetEnvironmentVariable("COMPUTERNAME")
                 ?? Environment.GetEnvironmentVariable("HOSTNAME")
#if !NETSTANDARD1_3
                 ?? Environment.MachineName
#endif
                ;

            var guid = Guid.NewGuid().ToString();

#if !NETSTANDARD1_3
            if (!String.IsNullOrWhiteSpace(serverName))
            {
                serverName += ":" + Process.GetCurrentProcess().Id;
            }
#endif

            return !String.IsNullOrWhiteSpace(serverName) ? $"{serverName.ToLowerInvariant()}:{guid}" : guid;
        }

        private void CreateServer(BackgroundServerContext context)
        {
            _logger.Trace($"{GetServerTemplate(context.ServerId)} is announcing itself...");

            var stopwatch = Stopwatch.StartNew();

            using (var connection = _storage.GetConnection())
            {
                connection.AnnounceServer(context.ServerId, GetServerContext(_properties));
            }

            stopwatch.Stop();

            ServerJobCancellationToken.AddServer(context.ServerId);
            _logger.Info($"{GetServerTemplate(context.ServerId)} successfully announced in {stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        private void ServerDelete(BackgroundServerContext context, Stopwatch stoppedAt)
        {
            try
            {
                _logger.Trace($"{GetServerTemplate(context.ServerId)} is reporting itself as stopped...");
                ServerJobCancellationToken.RemoveServer(context.ServerId);

                var stopwatch = Stopwatch.StartNew();

                using (var connection = _storage.GetConnection())
                {
                    connection.RemoveServer(context.ServerId);
                }

                stopwatch.Stop();

                _logger.Info($"{GetServerTemplate(context.ServerId)} successfully reported itself as stopped in {stopwatch.Elapsed.TotalMilliseconds} ms");
                _logger.Info($"{GetServerTemplate(context.ServerId)} has been stopped in total {stoppedAt?.Elapsed.TotalMilliseconds ?? 0} ms");
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                _logger.WarnException($"{GetServerTemplate(context.ServerId)} there was an exception, server may not be removed", ex);
            }
        }

        private void StartDispatchers(BackgroundServerContext context, ICollection<IBackgroundDispatcher> dispatchers)
        {
            if (_dispatcherBuilders.Length == 0)
            {
                throw new InvalidOperationException("No dispatchers registered for the processing server.");
            }

            _logger.Info($"{GetServerTemplate(context.ServerId)} is starting the registered dispatchers: {String.Join(", ", _dispatcherBuilders.Select(static builder => $"{builder}"))}...");

            foreach (var dispatcherBuilder in _dispatcherBuilders)
            {
                dispatchers.Add(dispatcherBuilder.Create(context, _options));
            }

            _logger.Info($"{GetServerTemplate(context.ServerId)} all the dispatchers started");
        }

        private void WaitForDispatchers(
            BackgroundServerContext context,
            IReadOnlyList<IBackgroundDispatcher> dispatchers)
        {
            if (dispatchers.Count == 0) return;

            var waitTasks = new Task[dispatchers.Count];

            for (var i = 0; i < dispatchers.Count; i++)
            {
                waitTasks[i] = dispatchers[i].WaitAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);
            }

            var nonStopped = new List<IBackgroundDispatcher>();

            try
            {
                Task.WaitAll(waitTasks, context.ShutdownToken);
            }
            catch (OperationCanceledException)
            {
                for (var i = 0; i < dispatchers.Count; i++)
                {
                    if (waitTasks[i].Status != TaskStatus.RanToCompletion)
                    {
                        nonStopped.Add(dispatchers[i]);
                    }
                }
            }

            if (nonStopped.Count > 0)
            {
                var nonStoppedNames = nonStopped.Select(static dispatcher => $"{dispatcher}").ToArray();
                _logger.Warn($"{GetServerTemplate(context.ServerId)} stopped non-gracefully due to {String.Join(", ", nonStoppedNames)}. Outstanding work on those dispatchers could be aborted, and there can be delays in background processing. This server instance will be incorrectly shown as active for a while. To avoid non-graceful shutdowns, investigate what prevents from stopping gracefully and add CancellationToken support for those methods.");
            }
            else
            {
                _logger.Info($"{GetServerTemplate(context.ServerId)} All dispatchers stopped");
            }
        }

        private static void DisposeDispatchers(IEnumerable<IBackgroundDispatcher> dispatchers)
        {
            foreach (var dispatcher in dispatchers)
            {
                dispatcher.Dispose();
            }
        }

        private static ServerContext GetServerContext(IDictionary<string, object> properties)
        {
            var serverContext = new ServerContext();

            if (properties.TryGetValue("Queues", out var queues) && queues is string[] array)
            {
                serverContext.Queues = array;
            }

            if (properties.TryGetValue("WorkerCount", out var workerCount))
            {
                serverContext.WorkerCount = (int)workerCount;
            }

            return serverContext;
        }

        internal static string GetServerTemplate(string serverId)
        {
            string name = serverId;

            try
            {
                var split = serverId.Split(ColonSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 3 && split[2].Length > 8)
                {
                    name = $"{split[0]}:{split[1]}:{split[2].Substring(0, 8)}";
                }
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                // ignored
            }

            return $"Server {name}";
        }
    }
}
