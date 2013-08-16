using System;
using System.Linq;
using System.Threading;

using ServiceStack.Redis;

namespace HangFire
{
    public class JobSchedulePoller : IDisposable
    {
        private readonly Thread _pollerThread;

        private readonly TimeSpan _pollInterval = Configuration.Instance.PollInterval;
        private readonly RedisClient _client = new RedisClient();

        private bool _stopped;
        private readonly object _locker = new object();

        public JobSchedulePoller()
        {
            _pollerThread = new Thread(Work) { IsBackground = true, Name = "HangFire.SchedulePoller" };
            _pollerThread.Start();
        }

        public void Dispose()
        {
            lock (_locker)
            {
                _stopped = true;
            }
            _pollerThread.Interrupt();
            _pollerThread.Join();
            _client.Dispose();
        }

        private void Work()
        {
            try
            {
                while (true)
                {
                    _client.TryToDo(redis =>
                    {
                        while (true)
                        {
                            lock (_locker)
                            {
                                if (_stopped) { return; }
                            }

                            if (!TryToPeekAndProcessNextJob(redis))
                            {
                                break;
                            }
                        }
                    });
                    Thread.Sleep(_pollInterval);
                }
            }
            catch (ThreadInterruptedException)
            {
            }
        }

        private static bool TryToPeekAndProcessNextJob(IRedisClient redis)
        {
            var now = DateTime.UtcNow.ToTimestamp();
            var scheduledJob =
                redis.GetRangeFromSortedSetByLowestScore("hangfire:schedule", Double.NegativeInfinity, now, 0, 1)
                    .FirstOrDefault();

            if (scheduledJob == null)
            {
                return false;
            }

            if (redis.RemoveItemFromSortedSet("hangfire:schedule", scheduledJob))
            {
                redis.EnqueueItemOnList("hangfire:queue:default", scheduledJob);
            }
            return true;
        }
    }
}
