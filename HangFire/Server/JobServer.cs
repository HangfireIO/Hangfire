using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class JobServer : IDisposable
    {
        private readonly string _serverName;

        private readonly int _concurrency;
        private readonly string _queue;
        private readonly Thread _managerThread;

        private readonly WorkerPool _pool;
        private readonly ThreadWrapper _schedulePoller;
        private readonly ThreadWrapper _fetchedJobsWatcher;
        private readonly JobFetcher _fetcher;

        private readonly IRedisClient _redis = RedisFactory.Create();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposed;

        private readonly ILog _logger = LogManager.GetLogger("HangFire.Manager");

        public JobServer(
            string machineName,
            string queue,
            int concurrency,
            TimeSpan pollInterval,
            JobActivator jobActivator)
        {
            if (String.IsNullOrEmpty(queue))
            {
                throw new ArgumentNullException("queue", "Please specify the queue name you want to listen.");
            }

            if (concurrency <= 0)
            {
                throw new ArgumentOutOfRangeException("concurrency", "Concurrency value can not be negative or zero.");
            }

            if (pollInterval != pollInterval.Duration())
            {
                throw new ArgumentOutOfRangeException("pollInterval", "Poll interval value must be positive.");
            }

            _concurrency = concurrency;
            _queue = queue;

            _serverName = String.Format("{0}:{1}", machineName, Process.GetCurrentProcess().Id);

            var jobInvoker = ServerJobInvoker.Current;

            _pool = new WorkerPool(
                new ServerContext(_serverName, _queue, concurrency),
                jobInvoker, jobActivator ?? new JobActivator());

            _fetcher = new JobFetcher(_redis, _queue);

            _managerThread = new Thread(Work)
                {
                    Name = "HangFire.Manager",
                    IsBackground = true
                };
            _managerThread.Start();

            _logger.Info("Manager thread has been started.");

            _schedulePoller = new ThreadWrapper(new SchedulePoller(pollInterval));
            _fetchedJobsWatcher = new ThreadWrapper(new DequeuedJobsWatcher());
        }

        /// <summary>
        /// Stops to processing the queue and stops all the workers.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _fetchedJobsWatcher.Dispose();
            _schedulePoller.Dispose();

            _logger.Info("Stopping manager thread...");
            _cts.Cancel();
            _managerThread.Join();

            _pool.Dispose();
            _cts.Dispose();
        }

        public static void RemoveFromFetchedQueue(
            IRedisClient redis, string jobId, string queue)
        {
            using (var transaction = redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromList(
                    String.Format("hangfire:queue:{0}:dequeued", queue),
                    jobId,
                    -1));

                transaction.QueueCommand(x => x.RemoveEntry(
                    String.Format("hangfire:job:{0}:fetched", jobId),
                    String.Format("hangfire:job:{0}:checked", jobId)));

                transaction.Commit();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
        private void Work()
        {
            try
            {
                AnnounceServer();

                if (_cts.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                while (true)
                {
                    var worker = _pool.TakeFree(_cts.Token);

                    string jobId;

                    do
                    {
                        jobId = _fetcher.DequeueJobId();
                        if (jobId == null && _cts.IsCancellationRequested)
                        {
                            throw new OperationCanceledException();
                        }
                    } while (jobId == null);

                    worker.Process(jobId);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Info("Shutdown has been requested. Exiting...");
                HideServer();
            }
            catch (Exception ex)
            {
                _logger.Fatal("Unexpected exception caught in the manager thread. Jobs will not be processed.", ex);
            }
        }

        private void AnnounceServer()
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.AddItemToSet(
                    "hangfire:servers", _serverName));

                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format("hangfire:server:{0}", _serverName),
                    new Dictionary<string, string>
                        {
                            { "Workers", _concurrency.ToString() },
                            { "StartedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) }
                        }));

                foreach (var queue in new[] { _queue })
                {
                    var queueName = queue;
                    transaction.QueueCommand(x => x.AddItemToSet(
                        String.Format("hangfire:server:{0}:queues", _serverName),
                        queueName));
                }

                transaction.Commit();
            }
        }

        private void HideServer()
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromSet(
                    "hangfire:servers",
                    _serverName));

                transaction.QueueCommand(x => x.RemoveEntry(
                    String.Format("hangfire:server:{0}", _serverName),
                    String.Format("hangfire:server:{0}:queues", _serverName)));

                transaction.Commit();
            }
        }
    }
}