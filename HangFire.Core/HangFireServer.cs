using System;
using System.Collections.Concurrent;
using System.Threading;

using ServiceStack.Logging;

namespace HangFire
{
    public class HangFireServer : IDisposable
    {
        private readonly string _serverName;
        private readonly int _concurrency;
        private readonly string _queueName;
        private readonly Thread _managerThread;
        private readonly Thread _completionHandlerThread;
        private readonly JobDispatcherPool _pool;
        private readonly JobSchedulePoller _schedule;
        private readonly RedisClient _blockingClient = new RedisClient();
        private readonly RedisClient _client = new RedisClient();
        private readonly BlockingCollection<string> _completedJobs 
            = new BlockingCollection<string>();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposed;

        private readonly ILog _logger = LogManager.GetLogger("HangFire.Manager");

        public HangFireServer(string serverName)
            : this(serverName, "default")
        {
        }

        public HangFireServer(string serverName, string queueName)
            : this(serverName, queueName, Environment.ProcessorCount * 2)
        {
        }

        public HangFireServer(string serverName, string queueName, int concurrency)
            : this(serverName, queueName, concurrency, TimeSpan.FromSeconds(15))
        {
        }

        public HangFireServer(string serverName, string queueName, int concurrency, TimeSpan pollInterval)
        {
            if (String.IsNullOrEmpty(serverName))
            {
                throw new ArgumentNullException("serverName", "You must provide non-null and unique server name.");
            }

            if (String.IsNullOrEmpty(queueName))
            {
                throw new ArgumentNullException("queueName", "Please specify the queue name you want to listen.");
            }

            if (concurrency <= 0)
            {
                throw new ArgumentOutOfRangeException("concurrency", "Concurrency value can not be negative or zero.");
            }

            if (pollInterval != pollInterval.Duration())
            {
                throw new ArgumentOutOfRangeException("pollInterval", "Poll interval value must be positive.");
            }

            _serverName = serverName;
            _concurrency = concurrency;
            _queueName = queueName;

            _completionHandlerThread = new Thread(HandleCompletedJobs)
                {
                    Name = "HangFire.CompletionHandler",
                    IsBackground = true
                };

            _completionHandlerThread.Start();
            
            _pool = new JobDispatcherPool(concurrency, serverName);
            _pool.JobCompleted += PoolOnJobCompleted;

            _managerThread = new Thread(Work)
                {
                    Name = "HangFire.Manager",
                    IsBackground = true
                };
            _managerThread.Start();

            _logger.Info("Manager thread has been started.");

            _schedule = new JobSchedulePoller(pollInterval);
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
            
            _completionHandlerThread.Join();

            _completedJobs.Dispose();
            _cts.Dispose();

            _blockingClient.Dispose();
            _client.Dispose();
        }

        private void Work()
        {
            try
            {
                _blockingClient.TryToDo(x => x.AnnounceServer(_serverName, _concurrency, _queueName));

                _logger.Info("Starting to requeue processing jobs...");
                int requeued = 0;
                bool finished = false;
                while (true)
                {
                    _blockingClient.TryToDo(storage =>
                        {
                            requeued += storage.RequeueProcessingJobs(_serverName, _queueName, _cts.Token);
                            finished = true;
                        });
                    if (finished) break;
                }
                _logger.Info(String.Format("Requeued {0} jobs.", requeued));

                if (_cts.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                while (true)
                {
                    var dispatcher = _pool.TakeFree(_cts.Token);

                    _blockingClient.TryToDo(
                        storage =>
                        {
                            string job;

                            do
                            {
                                job = storage.DequeueJob(_serverName, _queueName, TimeSpan.FromSeconds(5));

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
                _blockingClient.TryToDo(x => x.HideServer(_serverName, _queueName));
            }
            catch (Exception ex)
            {
                _logger.Fatal("Unexpected exception caught in the manager thread. Jobs will not be processed.", ex);
            }
        }

        private void HandleCompletedJobs()
        {
            try
            {
                while (true)
                {
                    var completedJob = _completedJobs.Take(_cts.Token);
                    bool removed = false;
                    while (true)
                    {
                        _client.TryToDo(storage =>
                            {
                                storage.RemoveProcessingJob(_serverName, _queueName, completedJob);
                                removed = true;
                            });
                        if (removed) break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.Fatal("Unexpected exception.", ex);
            }
        }

        private void PoolOnJobCompleted(object sender, JobCompletedEventArgs args)
        {
            _completedJobs.Add(args.SerializedJob);
        }
    }
}