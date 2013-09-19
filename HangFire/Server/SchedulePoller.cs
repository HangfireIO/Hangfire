using System;
using System.Threading;

using HangFire.Storage;

using ServiceStack.Logging;

namespace HangFire.Server
{
    internal class SchedulePoller : IDisposable
    {
        private readonly ILog _logger = LogManager.GetLogger("SchedulePoller");
        private readonly Thread _pollerThread;

        private readonly TimeSpan _pollInterval;
        private readonly RedisStorage _redis = new RedisStorage();

        private bool _stopped;
        private readonly object _locker = new object();

        public SchedulePoller(TimeSpan pollInterval)
        {
            _pollInterval = pollInterval;
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
            _redis.Dispose();
        }

        private void Work()
        {
            try
            {
                while (true)
                {
                    lock (_locker)
                    {
                        if (_stopped) { return; }

                        bool wasScheduled = false;
                        _redis.RetryOnRedisException(x => wasScheduled = x.EnqueueScheduledJob(DateTime.UtcNow));

                        if (!wasScheduled)
                        {
                            Thread.Sleep(_pollInterval);
                        }
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
            }
            catch (Exception ex)
            {
                _logger.Fatal(
                    "Scheduled jobs will not be added to their queues by this server instance: "
                    + "unexpected exception caught in the SchedulePoller thread.",
                    ex);
            }
        }
    }
}
