// This file is part of Hangfire. Copyright © 2019 Hangfire OÜ.
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
using System.Diagnostics;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.Server
{
    internal sealed class ServerHeartbeatProcess : IBackgroundProcess
    {
        private readonly ILog _logger = LogProvider.GetLogger(typeof(ServerHeartbeatProcess));

        private readonly TimeSpan _interval;
        private readonly TimeSpan _serverTimeout;
        private readonly Action _requestRestart;
        private Stopwatch _faultedSince;

        public ServerHeartbeatProcess(TimeSpan interval, TimeSpan serverTimeout, Action requestRestart)
        {
            _interval = interval;
            _serverTimeout = serverTimeout;
            _requestRestart = requestRestart;
        }

        public void Execute(BackgroundProcessContext context)
        {
            _logger.Trace($"{BackgroundServerProcess.GetServerTemplate(context.ServerId)} waiting for {_interval} delay before sending a heartbeat");

            context.ShutdownToken.WaitOrThrow(_interval);

            try
            {
                using (var connection = context.Storage.GetConnection())
                {
                    connection.Heartbeat(context.ServerId);
                }

                if (_faultedSince == null)
                {
                    _logger.Debug($"{BackgroundServerProcess.GetServerTemplate(context.ServerId)} heartbeat successfully sent");
                }
                else
                {
                    _logger.Info($"{BackgroundServerProcess.GetServerTemplate(context.ServerId)} is now able to continue sending heartbeats");
                    _faultedSince = null;
                }
            }
            catch (BackgroundServerGoneException)
            {
                if (!context.ShutdownToken.IsCancellationRequested)
                {
                    _logger.Warn($"{BackgroundServerProcess.GetServerTemplate(context.ServerId)} was considered dead by other servers, restarting...");
                    _requestRestart();
                }

                return;
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                _logger.WarnException($"{BackgroundServerProcess.GetServerTemplate(context.ServerId)} encountered an exception while sending heartbeat", ex);

                if (_faultedSince == null) _faultedSince = Stopwatch.StartNew();
                if (_faultedSince.Elapsed >= _serverTimeout)
                {
                    _logger.Error($"{BackgroundServerProcess.GetServerTemplate(context.ServerId)} will be restarted due to server time out");

                    _requestRestart();
                    return;
                }
            }
        }
    }
}