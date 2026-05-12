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
using System.Collections.Generic;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.Server
{
    internal sealed class ServerResourceCommandProcess : IBackgroundProcess
    {
        private readonly IJobServerDrainController _drainController;
        private readonly IJobServerQueueDrainController _queueDrainController;
        private readonly TimeSpan _pollingInterval;
        private readonly ILog _logger = LogProvider.GetLogger(typeof(ServerResourceCommandProcess));

        public ServerResourceCommandProcess(
            IJobServerDrainController drainController,
            IJobServerQueueDrainController queueDrainController,
            TimeSpan pollingInterval)
        {
            _drainController = drainController ?? throw new ArgumentNullException(nameof(drainController));
            _queueDrainController = queueDrainController;
            if (pollingInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(pollingInterval));

            _pollingInterval = pollingInterval;
        }

        public void Execute(BackgroundProcessContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            try
            {
                using (var connection = context.Storage.GetConnection())
                {
                    if (!(connection is JobStorageConnection storageConnection))
                    {
                        context.ShutdownToken.WaitOrThrow(_pollingInterval);
                        return;
                    }

                    var command = storageConnection.GetServerResourceCommand(context.ServerId);
                    if (command == null)
                    {
                        context.ShutdownToken.WaitOrThrow(_pollingInterval);
                        return;
                    }

                    ApplyCommand(command);
                    storageConnection.AddServerResourceEvent(CreateEvent(context.ServerId, command, "command-observed", null));
                    storageConnection.ClearServerResourceCommand(context.ServerId, command.CommandId);
                    storageConnection.UpdateServer(context.ServerId, BackgroundServerProcess.GetServerContext(CopyProperties(context)));
                }
            }
            catch (OperationCanceledException) when (context.ShutdownToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                _logger.WarnException(
                    $"{BackgroundServerProcess.GetServerTemplate(context.ServerId)} encountered an exception while applying a resource command",
                    ex);
            }

            context.ShutdownToken.WaitOrThrow(_pollingInterval);
        }

        private void ApplyCommand(ServerResourceCommand command)
        {
            switch (command.Command)
            {
                case "drain":
                    _drainController.Drain(command.Reason);
                    break;
                case "resume":
                    _drainController.Resume();
                    break;
                case "drain-queue":
                    if (_queueDrainController == null)
                    {
                        throw new NotSupportedException("Current resource does not support queue drain commands.");
                    }

                    _queueDrainController.DrainQueue(command.Queue, command.Reason);
                    break;
                case "resume-queue":
                    if (_queueDrainController == null)
                    {
                        throw new NotSupportedException("Current resource does not support queue drain commands.");
                    }

                    _queueDrainController.ResumeQueue(command.Queue);
                    break;
                default:
                    _logger.Warn($"Unknown server resource command '{command.Command}' was ignored.");
                    break;
            }
        }

        private static ServerResourceEvent CreateEvent(
            string serverId,
            ServerResourceCommand command,
            string eventType,
            string allocationState)
        {
            return new ServerResourceEvent
            {
                ServerId = serverId,
                EventType = eventType,
                AllocationState = allocationState,
                Queue = command.Queue,
                Reason = command.Reason,
                CreatedAt = DateTime.UtcNow,
                Source = String.IsNullOrWhiteSpace(command.CreatedBy) ? "remote-command" : command.CreatedBy
            };
        }

        private static IDictionary<string, object> CopyProperties(BackgroundProcessContext context)
        {
            var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in context.Properties)
            {
                properties.Add(property.Key, property.Value);
            }

            return properties;
        }
    }
}
