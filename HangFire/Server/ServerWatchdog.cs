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

        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);

        private readonly IRedisClient _redis = RedisFactory.CreateClient();
        private readonly ILog _logger = LogManager.GetLogger(typeof (ServerWatchdog));

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

            foreach (var heartbeat in heartbeats)
            {
                var maxTime = new DateTime(
                    Math.Max(heartbeat.Value.Item1.Ticks, (heartbeat.Value.Item2 ?? DateTime.MinValue).Ticks));

                if (utcNow > maxTime.Add(ServerTimeout))
                {
                    JobServer.RemoveServer(_redis, heartbeat.Key);
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
                while (true)
                {
                    RemoveTimedOutServers();
                    if (_stopped.WaitOne(CheckInterval))
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Fatal("Unexpected exception caught.", ex);
            }
        }

        void IThreadWrappable.Dispose(Thread thread)
        {
            _stopped.Set();
            thread.Join();
        }
    }
}
