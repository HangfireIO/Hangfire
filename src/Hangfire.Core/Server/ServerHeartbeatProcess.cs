// This file is part of Hangfire. Copyright © 2019 Sergey Odinokov.
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

            context.ShutdownToken.Wait(_interval);
            context.ShutdownToken.ThrowIfCancellationRequested();

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
            catch (Exception ex)
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