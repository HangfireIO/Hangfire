using System;
using System.Threading;

using ServiceStack.Logging;

namespace HangFire
{
    public class JobManager : IDisposable
    {
        private readonly string _iid;
        private readonly string _queue;
        private readonly Thread _managerThread;
        private readonly JobDispatcherPool _pool;
        private readonly JobSchedulePoller _schedule;
        private readonly RedisClient _client = new RedisClient();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposed;

        private readonly ILog _logger = LogManager.GetLogger("HangFire.Manager");

        public JobManager(string iid, int concurrency, string queue)
        {
            _iid = iid;
            _queue = queue;
            _managerThread = new Thread(Work)
                {
                    Name = "HangFire.Manager", 
                    IsBackground = true
                };

            _pool = new JobDispatcherPool(concurrency);
            _managerThread.Start();

            _logger.Info("Manager thread has been started.");

            _schedule = new JobSchedulePoller();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _schedule.Dispose();

            _logger.Info("Stopping manager thread...");
            _cts.Cancel();
            _managerThread.Join();

            _pool.Dispose();
            _cts.Dispose();
            _client.Dispose();
        }

        private void Work()
        {
            try
            {
                _logger.Info("Starting to requeue processing jobs...");
                int requeued = 0;
                bool finished = false;
                while (true)
                {
                    _client.TryToDo(storage =>
                        {
                            requeued += storage.RequeueProcessingJobs(_iid, _queue, _cts.Token);
                            finished = true;
                        });
                    if (finished) break;
                }
                _logger.Info(String.Format("Requeued {0} jobs.", requeued));

                if (_cts.IsCancellationRequested)
                {
                    return;
                }

                while (true)
                {
                    var dispatcher = _pool.TakeFree(_cts.Token);

                    _client.TryToDo(
                        storage =>
                        {
                            string job;

                            do
                            {
                                job = storage.DequeueJob(_iid, _queue, TimeSpan.FromSeconds(5));

                                if (job == null && _cts.IsCancellationRequested)
                                {
                                    throw new OperationCanceledException();
                                }
                            } while (job == null);

                            dispatcher.Process(job);
                        });
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Info("Shutdown has been requested. Exiting...");
            }
            catch (Exception ex)
            {
                _logger.Fatal("Unexpected exception caught in the manager thread. Jobs will not be processed.", ex);
            }
        }
    }
}