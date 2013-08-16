using System;
using System.Linq;
using System.Threading;

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
            while (true)
            {
                try
                {
                    var redis = _client.Connection;

                    while (true)
                    {
                        lock (_locker)
                        {
                            if (_stopped)
                            {
                                return;
                            }
                        }

                        var now = DateTime.UtcNow.ToTimestamp();
                        var scheduledJob =
                            redis.GetRangeFromSortedSetByLowestScore(
                                "hangfire:schedule",
                                Double.NegativeInfinity,
                                now,
                                0,
                                1).FirstOrDefault();

                        if (scheduledJob == null)
                        {
                            break;
                        }

                        if (redis.RemoveItemFromSortedSet("hangfire:schedule", scheduledJob))
                        {
                            redis.EnqueueItemOnList("hangfire:queue:default", scheduledJob);
                        }
                    }

                    Thread.Sleep(_pollInterval);
                }
                catch (ThreadInterruptedException)
                {
                }
                catch (Exception)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    _client.Reconnect();
                    throw;
                }
            }
        }
    }
}
