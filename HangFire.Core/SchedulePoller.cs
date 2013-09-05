using System;
using System.Threading;

namespace HangFire
{
    internal class SchedulePoller : IDisposable
    {
        private readonly Thread _pollerThread;

        private readonly TimeSpan _pollInterval;
        private readonly RedisClient _client = new RedisClient();

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
            _client.Dispose();
        }

        private void Work()
        {
            try
            {
                while (true)
                {
                    _client.TryToDo(storage =>
                    {
                        while (true)
                        {
                            lock (_locker)
                            {
                                if (_stopped) { return; }
                            }

                            var now = DateTime.UtcNow.ToTimestamp();

                            var scheduledJob = storage.GetScheduledJob(now);
                            if (scheduledJob != null)
                            {
                                var job = JsonHelper.Deserialize<JobDescription>(scheduledJob);
                                var queue = HangFireJob.GetQueueName(job.WorkerType);
                                storage.EnqueueJob(queue, scheduledJob);
                            }
                            else
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
    }
}
