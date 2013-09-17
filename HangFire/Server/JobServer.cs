using System;
using System.Collections.Concurrent;
using System.Threading;
using HangFire.Storage;
using ServiceStack.Logging;

namespace HangFire.Server
{
    internal class JobServer : IDisposable
    {
        private readonly string _serverName;
        private readonly int _concurrency;
        private readonly string _queueName;
        private readonly Thread _managerThread;
        private readonly Thread _completionHandlerThread;
        private readonly ThreadedWorkerManager _pool;
        private readonly SchedulePoller _schedule;
        private readonly RedisStorage _blockingRedis = new RedisStorage();
        private readonly RedisStorage _redis = new RedisStorage();
        private readonly BlockingCollection<string> _completedJobIds
            = new BlockingCollection<string>();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposed;

        private readonly ILog _logger = LogManager.GetLogger("HangFire.Manager");

        public JobServer(
            string serverName,
            string queueName,
            int concurrency,
            TimeSpan pollInterval,
            JobActivator jobActivator)
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

            var jobInvoker = ServerJobInvoker.Current;

            _pool = new ThreadedWorkerManager(
                new ServerContext(_serverName, _queueName, concurrency), 
                jobInvoker, jobActivator ?? new JobActivator());
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

            _completedJobIds.Dispose();
            _cts.Dispose();

            _blockingRedis.Dispose();
            _redis.Dispose();
        }

        private void Work()
        {
            try
            {
                _blockingRedis.AnnounceServer(_serverName, _concurrency, _queueName);

                _logger.Info("Starting to requeue processing jobs...");
                int requeued = 0;

                _blockingRedis.RetryOnRedisException(x =>
                    requeued += x.RequeueProcessingJobs(_serverName, _queueName, _cts.Token));

                _logger.Info(String.Format("Requeued {0} jobs.", requeued));

                if (_cts.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                while (true)
                {
                    var worker = _pool.TakeFree(_cts.Token);

                    string jobId = null;
                    _blockingRedis.RetryOnRedisException(
                        x =>
                        {
                            do
                            {
                                jobId = x.DequeueJobId(_serverName, _queueName, TimeSpan.FromSeconds(5));
                                if (jobId == null && _cts.IsCancellationRequested)
                                {
                                    throw new OperationCanceledException();
                                }
                            } while (jobId == null);
                        });

                    worker.Process(jobId);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Info("Shutdown has been requested. Exiting...");
                _blockingRedis.HideServer(_serverName, _queueName);
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
                    var jobId = _completedJobIds.Take(_cts.Token);

                    _redis.RetryOnRedisException(x =>
                        x.RemoveProcessingJob(_serverName, _queueName, jobId));
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
            _completedJobIds.Add(args.JobId);
        }
    }
}