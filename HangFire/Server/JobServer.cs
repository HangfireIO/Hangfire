using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class JobServer : IDisposable
    {
        private readonly ServerContext _context;

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
            IEnumerable<string> queues,
            int concurrency,
            TimeSpan pollInterval,
            JobActivator jobActivator)
        {
            if (queues == null)
            {
                throw new ArgumentNullException("queues");
            }

            if (concurrency <= 0)
            {
                throw new ArgumentOutOfRangeException("concurrency", "Concurrency value can not be negative or zero.");
            }

            if (pollInterval != pollInterval.Duration())
            {
                throw new ArgumentOutOfRangeException("pollInterval", "Poll interval value must be positive.");
            }

            var serverName = String.Format("{0}:{1}", machineName, Process.GetCurrentProcess().Id);

            _context = new ServerContext(
                serverName,
                queues.ToList(),
                concurrency,
                jobActivator ?? new JobActivator(),
                JobPerformer.Current);

            _pool = new WorkerPool(_context);
            _fetcher = new JobFetcher(_redis, _context.Queues);

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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
        private void Work()
        {
            try
            {
                AnnounceServer();

                _cts.Token.ThrowIfCancellationRequested();

                while (true)
                {
                    var worker = _pool.TakeFree(_cts.Token);

                    JobPayload jobId;

                    do
                    {
                        jobId = _fetcher.DequeueJob();
                        if (jobId == null)
                        {
                            _cts.Token.ThrowIfCancellationRequested();
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
                    "hangfire:servers", _context.ServerName));

                transaction.QueueCommand(x => x.SetRangeInHash(
                    String.Format("hangfire:server:{0}", _context.ServerName),
                    new Dictionary<string, string>
                        {
                            { "Workers", _context.WorkersCount.ToString() },
                            { "StartedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) }
                        }));

                foreach (var queue in _context.Queues)
                {
                    var queueName = queue;
                    transaction.QueueCommand(x => x.AddItemToSet(
                        String.Format("hangfire:server:{0}:queues", _context.ServerName),
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
                    _context.ServerName));

                transaction.QueueCommand(x => x.RemoveEntry(
                    String.Format("hangfire:server:{0}", _context.ServerName),
                    String.Format("hangfire:server:{0}:queues", _context.ServerName)));

                transaction.Commit();
            }
        }
    }
}