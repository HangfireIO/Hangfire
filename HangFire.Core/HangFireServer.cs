using System;
using System.Collections.Concurrent;
using System.Threading;

using ServiceStack.Logging;

namespace HangFire
{
    /// <summary>
    /// Represents the top-level class for job queue dispatching.
    /// </summary>
    public class HangFireServer : IDisposable
    {
        private readonly string _serverName;
        private readonly int _concurrency;
        private readonly string _queueName;
        private readonly Thread _managerThread;
        private readonly Thread _completionHandlerThread;
        private readonly ThreadedWorkerManager _pool;
        private readonly SchedulePoller _schedule;
        private readonly RedisClient _blockingClient = new RedisClient();
        private readonly RedisClient _client = new RedisClient();
        private readonly BlockingCollection<string> _completetJobIds 
            = new BlockingCollection<string>();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposed;

        private readonly ILog _logger = LogManager.GetLogger("HangFire.Manager");

        /// <summary>
        /// Initializes and starts a new instance of the <see cref="HangFireServer"/> class
        /// with a specified server name.
        /// </summary>
        /// <param name="serverName">Server name.</param>
        public HangFireServer(string serverName)
            : this(serverName, "default")
        {
        }

        /// <summary>
        /// Initializes and starts a new instance of the <see cref="HangFireServer"/> class
        /// with a specified server name and queue name that will be processed.
        /// </summary>
        /// <param name="serverName">Server name.</param>
        /// <param name="queueName">Processing queue name.</param>
        public HangFireServer(string serverName, string queueName)
            : this(serverName, queueName, Environment.ProcessorCount * 2)
        {
        }

        /// <summary>
        /// Initializes and starts a new instance of the <see cref="HangFireServer"/> class
        /// with a specified server name, queue name, amount of workers.
        /// </summary>
        /// <param name="serverName">Server name.</param>
        /// <param name="queueName">Processing queue name.</param>
        /// <param name="concurrency">Amount of workers that will work in parallel.</param>
        public HangFireServer(string serverName, string queueName, int concurrency)
            : this(serverName, queueName, concurrency, TimeSpan.FromSeconds(15))
        {
        }

        /// <summary>
        /// Initializes and starts a new instance of the <see cref="HangFireServer"/> class
        /// with a specified server name, queue name, amount of workers and
        /// polling interval.
        /// </summary>
        /// <param name="serverName">Server name.</param>
        /// <param name="queueName">Processing queue name.</param>
        /// <param name="concurrency">Amount of workers that will work in parallel.</param>
        /// <param name="pollInterval">Polling interval for scheduled jobs.</param>
        public HangFireServer(string serverName, string queueName, int concurrency, TimeSpan pollInterval)
            : this(serverName, queueName, concurrency, pollInterval, null)
        {
        }

        /// <summary>
        /// Initializes and starts a new instance of the <see cref="HangFireServer"/> class
        /// with a specified server name, queue name, amount of workers, polling interval
        /// and an instance of the <see cref="HangFireJobActivator"/> class.
        /// </summary>
        /// <param name="serverName">Server name.</param>
        /// <param name="queueName">Processing queue name.</param>
        /// <param name="concurrency">Amount of workers that will work in parallel.</param>
        /// <param name="pollInterval">Polling interval for scheduled jobs.</param>
        /// <param name="jobActivator">Instance of the <see cref="HangFireJobActivator"/> that will be used to activate jobs.</param>
        public HangFireServer(
            string serverName, 
            string queueName, 
            int concurrency, 
            TimeSpan pollInterval, 
            HangFireJobActivator jobActivator)
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
            
            _pool = new ThreadedWorkerManager(concurrency, serverName, jobActivator ?? new HangFireJobActivator());
            _pool.JobCompleted += PoolOnJobCompleted;

            _managerThread = new Thread(Work)
                {
                    Name = "HangFire.Manager",
                    IsBackground = true
                };
            _managerThread.Start();

            _logger.Info("Manager thread has been started.");

            _schedule = new SchedulePoller(pollInterval);
        }

        /// <summary>
        /// Stops to processing the queue and stops all the workers.
        /// </summary>
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

            _completetJobIds.Dispose();
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
                            string jobId;

                            do
                            {
                                jobId = storage.DequeueJobId(_serverName, _queueName, TimeSpan.FromSeconds(5));

                                if (jobId == null && _cts.IsCancellationRequested)
                                {
                                    throw new OperationCanceledException();
                                }
                            } while (jobId == null);

                            dispatcher.Process(jobId);
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
                    var jobId = _completetJobIds.Take(_cts.Token);
                    bool removed = false;
                    while (true)
                    {
                        _client.TryToDo(storage =>
                            {
                                storage.RemoveProcessingJob(_serverName, _queueName, jobId);
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
            _completetJobIds.Add(args.JobId);
        }
    }
}