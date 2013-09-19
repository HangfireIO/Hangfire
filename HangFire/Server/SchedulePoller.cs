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

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public SchedulePoller(TimeSpan pollInterval)
        {
            _pollInterval = pollInterval;
            _pollerThread = new Thread(Work) { IsBackground = true, Name = "HangFire.SchedulePoller" };
            _pollerThread.Start();
        }

        public void Dispose()
        {
            _cts.Cancel();

            _pollerThread.Interrupt();
            _pollerThread.Join();
            _redis.Dispose();

            _cts.Dispose();
        }

        private void Work()
        {
            try
            {
                while (true)
                {
                    bool wasScheduled = false;
                    _redis.RetryOnRedisException(
                        x => wasScheduled = x.EnqueueScheduledJob(DateTime.UtcNow),
                        _cts.Token);

                    if (!wasScheduled)
                    {
                        Thread.Sleep(_pollInterval);
                    }
                }
            }
            catch (OperationCanceledException)
            {
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
