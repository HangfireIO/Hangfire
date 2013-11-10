// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class ServerWatchdog : IThreadWrappable, IDisposable
    {
        private static readonly TimeSpan ServerTimeout = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServerWatchdog));

        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);

        private readonly IRedisClient _redis;
        
        public ServerWatchdog(IRedisClientsManager redisManager)
        {
            _redis = redisManager.GetClient();
        }

        public void RemoveTimedOutServers()
        {
            var serverNames = _redis.GetAllItemsFromSet("hangfire:servers");
            var heartbeats = new Dictionary<string, Tuple<DateTime, DateTime?>>();

            var utcNow = DateTime.UtcNow;

            using (var pipeline = _redis.CreatePipeline())
            {
                foreach (var serverName in serverNames)
                {
                    var name = serverName;

                    pipeline.QueueCommand(
                        x => x.GetValuesFromHash(
                            String.Format("hangfire:server:{0}", name),
                            "StartedAt", "Heartbeat"),
                        x => heartbeats.Add(
                            name, 
                            new Tuple<DateTime, DateTime?>(
                                JobHelper.FromStringTimestamp(x[0]),
                                JobHelper.FromNullableStringTimestamp(x[1]))));
                }

                pipeline.Flush();
            }

            Logger.DebugFormat("Looking for timed out servers...");

            foreach (var heartbeat in heartbeats)
            {
                var maxTime = new DateTime(
                    Math.Max(heartbeat.Value.Item1.Ticks, (heartbeat.Value.Item2 ?? DateTime.MinValue).Ticks));

                if (utcNow > maxTime.Add(ServerTimeout))
                {
                    JobServer.RemoveServer(_redis, heartbeat.Key);
                    Logger.InfoFormat("Server '{0}' was removed due to time out.", heartbeat.Key);
                }
            }
        }

        public void Dispose()
        {
            _redis.Dispose();
        }

        void IThreadWrappable.Work()
        {
            try
            {
                Logger.Info("Server watchdog has been started.");

                while (true)
                {
                    JobServer.RetryOnException(RemoveTimedOutServers, _stopped);

                    if (_stopped.WaitOne(CheckInterval))
                    {
                        break;
                    }
                }

                Logger.Info("Server watchdog has been stopped.");
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unexpected exception caught.", ex);
            }
        }

        void IThreadWrappable.Dispose(Thread thread)
        {
            _stopped.Set();
            thread.Join();
        }
    }
}
