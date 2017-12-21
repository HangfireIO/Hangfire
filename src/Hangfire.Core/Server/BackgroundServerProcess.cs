// This file is part of Hangfire.
// Copyright © 2017 Sergey Odinokov.
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
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.Processing;

namespace Hangfire.Server
{
    internal sealed class BackgroundServerProcess : IBackgroundServerProcess
    {
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
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (dispatcherBuilders == null) throw new ArgumentNullException(nameof(dispatcherBuilders));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            _storage = storage;
            _options = options;
            _properties = properties;

            var builders = new List<IBackgroundProcessDispatcherBuilder>();
            builders.AddRange(GetRequiredProcesses());
            builders.AddRange(GetStorageComponents());
            builders.AddRange(dispatcherBuilders);

            _dispatcherBuilders = builders.ToArray();
        }

        public void Execute(CancellationToken shutdownToken, CancellationToken abortShutdownToken)
        {
            var serverId = GetGloballyUniqueServerId();
            Stopwatch stoppedAt = null;

            //using (LogProvider.OpenMappedContext("ServerId", serverId.ToString()))
            using (var restartCts = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken))
            using (var forcedRestartCts = CancellationTokenSource.CreateLinkedTokenSource(abortShutdownToken))
            using (restartCts.Token.Register(() => stoppedAt = Stopwatch.StartNew()))
            {
                var context = new BackgroundProcessContext(
                    serverId,
                    _storage,
                    _properties,
                    restartCts.Token,
                    forcedRestartCts.Token);

                var dispatchers = new List<IBackgroundDispatcher>();

                CreateServer(context);

                if (!shutdownToken.IsCancellationRequested)
                {
                    try
                    {
                        // todo what if there are no dispatchers?
                        StartDispatchers(context, dispatchers);
                        RunHeartbeatLoop(context);
                    }
                    catch (Exception ex)
                    {
                        // todo runheartbeatloop exception?
                        // todo exception can be caused by RunHeartbeatLoop
                        HandleDispatcherException(context, ex);
                    }
                    finally
                    {
                        restartCts.Cancel();

                        // todo needed for restarts only, to not to hang undefinitely while waiting for dispatchers
                        forcedRestartCts.CancelAfter(_options.RestartTimeout);
                    }
                }

                try
                {
                    WaitForDispatchers(context, dispatchers);
                    ServerDelete(context, stoppedAt);
                    // todo: set flag with non-graceful shutdown in storage to allow others to process its jobs
                }
                catch (Exception ex)
                {
                    _logger.WarnException($"{GetServerTemplate(serverId)} there was an exception, server may not be removed", ex);
                }
                finally
                {
                    DisposeDispatchers(dispatchers);
                }
            }
        }

        private IEnumerable<IBackgroundProcessDispatcherBuilder> GetRequiredProcesses()
        {
            yield return new ServerWatchdog(_options.ServerCheckInterval, _options.ServerTimeout).UseBackgroundPool(1);
        }

        private IEnumerable<IBackgroundProcessDispatcherBuilder> GetStorageComponents()
        {
            return _storage.GetComponents().Select(component => new ServerProcessDispatcherBuilder(
                component, 
                threadStart => BackgroundProcessExtensions.DefaultThreadFactory(1, component.GetType().Name, threadStart)));
        }

        private string GetGloballyUniqueServerId()
        {
            var serverName = _options.ServerName
                             ?? Environment.GetEnvironmentVariable("COMPUTERNAME")
                             ?? Environment.GetEnvironmentVariable("HOSTNAME");

            var guid = Guid.NewGuid().ToString();

#if NETFULL
            if (!String.IsNullOrWhiteSpace(serverName))
            {
                serverName += ":" + Process.GetCurrentProcess().Id;
            }
#endif

            return !String.IsNullOrWhiteSpace(serverName)
                ? $"{serverName.ToLowerInvariant()}:{guid}"
                : guid;
        }

        private void CreateServer(BackgroundProcessContext context)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                _logger.Trace($"{GetServerTemplate(context.ServerId)} is announcing itself");

                using (var connection = _storage.GetConnection())
                {
                    connection.AnnounceServer(context.ServerId, GetServerContext(_properties));
                }

                _logger.Info($"{GetServerTemplate(context.ServerId)} successfully announced in {{{stopwatch.Elapsed.TotalMilliseconds}}} ms");
            }
            catch (Exception)
            {
                // TODO: Change storage exception
                //throw new StorageException($"{GetServerTemplate(context.ServerId)} was failed to announce itself due to an exception", ex);
                throw;
            }
        }

        private static ServerContext GetServerContext(IDictionary<string, object> properties)
        {
            var serverContext = new ServerContext();

            if (properties.ContainsKey("Queues"))
            {
                var array = properties["Queues"] as string[];
                if (array != null)
                {
                    serverContext.Queues = array;
                }
            }

            if (properties.ContainsKey("WorkerCount"))
            {
                serverContext.WorkerCount = (int)properties["WorkerCount"];
            }
            return serverContext;
        }

        private void ServerDelete(BackgroundProcessContext context, Stopwatch stoppedAt)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Trace($"{GetServerTemplate(context.ServerId)} is reporting itself as stopped...");

            // todo CT.None for last=chance
            do
            {
                using (var connection = _storage.GetConnection())
                {
                    // todo check for CT
                    connection.RemoveServer(context.ServerId);
                }

                // todo make reachable
                _logger.Info(
                    $"{GetServerTemplate(context.ServerId)} successfully reported itself as stopped in {{{stopwatch.Elapsed.TotalMilliseconds}}} ms");
                _logger.Info(
                    $"{GetServerTemplate(context.ServerId)} has been stopped in total {{{stoppedAt?.Elapsed.TotalMilliseconds ?? 0}}} ms");
                return;
            } while (!context.AbortToken.IsCancellationRequested);
        }

        private void StartDispatchers(BackgroundProcessContext context, ICollection<IBackgroundDispatcher> dispatchers)
        {
            if (_dispatcherBuilders.Length == 0)
            {
                _logger.Warn($"{GetServerTemplate(context.ServerId)} no dispatcher builders registered, just sending heartbeats");
                return;
            }

            _logger.Info($"{GetServerTemplate(context.ServerId)} is starting the registered dispatchers: {String.Join(", ", _dispatcherBuilders.Select(builder => $"{{{builder}}}"))}...");

            foreach (var dispatcherBuilder in _dispatcherBuilders)
            {
                // todo use options
                dispatchers.Add(dispatcherBuilder.Create(context, _options));
            }

            _logger.Info($"{GetServerTemplate(context.ServerId)} all the dispatchers started");
        }

        private void HandleDispatcherException(BackgroundProcessContext context, Exception ex)
        {
            _logger.ErrorException($"{GetServerTemplate(context.ServerId)} caught an exception while creating dispatcher, will restart...", ex);
            ExceptionDispatchInfo.Capture(ex).Throw();
        }

        private void WaitForDispatchers(
            BackgroundProcessContext context,
            IReadOnlyList<IBackgroundDispatcher> dispatchers)
        {
            if (dispatchers.Count == 0) return;

            var waitTasks = new Task[dispatchers.Count];

            _logger.Info($"{GetServerTemplate(context.ServerId)} waiting for dispatchers to stop...");

            for (var i = 0; i < dispatchers.Count; i++)
            {
                waitTasks[i] = dispatchers[i].WaitAsync(context.AbortToken);
            }

            // todo exceptions
            Task.WaitAny(Task.WhenAll(waitTasks), context.AbortToken.AsTask());

            var nonStopped = new List<IBackgroundDispatcher>();

            for (var i = 0; i < dispatchers.Count; i++)
            {
                if (waitTasks[i].Status != TaskStatus.RanToCompletion)
                {
                    nonStopped.Add(dispatchers[i]);
                }
            }

            if (nonStopped.Count > 0)
            {
                var nonStoppedNames = nonStopped.Select(dispatcher => $"{{{dispatcher.ToString()}}}").ToArray();
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

        private void RunHeartbeatLoop(BackgroundProcessContext context)
        {
            var faultedSince = (Stopwatch)null;

            // avoid while true todo
            while (!context.CancellationToken.IsCancellationRequested)
            {
                try
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    // todo look for stopped dispatchers? Worth, when TaskScheduler or custom dispatchers don't catch threadaborts
                    // todo what if there are no dispatchers?

                    _logger.Trace(
                        $"{GetServerTemplate(context.ServerId)} waiting for {{{_options.HeartbeatInterval}}} delay before sending a heartbeat");

                    context.Wait(_options.HeartbeatInterval);

                    _logger.Trace($"{GetServerTemplate(context.ServerId)} is about to send a heartbeat...");

                    // todo use try
                    using (var connection = _storage.GetConnection())
                    {
                        connection.Heartbeat(context.ServerId);
                    }

                    // todo
                    /*_logger.Warn(
                        $"{GetServerTemplate(context.ServerId)} was considered dead by other servers, restarting...");
                    return;*/

                    if (faultedSince != null)
                    {
                        _logger.Info(
                            $"{GetServerTemplate(context.ServerId)} is now able to continue sending heartbeats");
                        faultedSince = null;
                    }
                }
                catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
                {
                    _logger.Info($"{GetServerTemplate(context.ServerId)} caught the stop signal, stopping the server...");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.WarnException($"{GetServerTemplate(context.ServerId)} encountered an exception while sending heartbeat", ex);

                    if (faultedSince == null) faultedSince = Stopwatch.StartNew();
                    if (faultedSince.Elapsed >= _options.ServerTimeout)
                    {
                        _logger.Error($"{GetServerTemplate(context.ServerId)} will be restarted due to server time out");
                        return;
                    }
                }
            }
        }

        private static string GetServerTemplate(string serverId)
        {
            return $"Server {{{serverId}}}";
        }
    }
}
