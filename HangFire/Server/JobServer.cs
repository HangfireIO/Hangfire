using System;
using System.Collections.Generic;
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
        private readonly string _queueName;
        private readonly Thread _managerThread;
        private readonly WorkerPool _pool;
        private readonly SchedulePoller _schedule;
        private readonly IRedisClient _redis = RedisFactory.Create();

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

            var jobInvoker = ServerJobInvoker.Current;

            _pool = new WorkerPool(
                new ServerContext(_serverName, _queueName, concurrency), 
                jobInvoker, jobActivator ?? new JobActivator());

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
            _cts.Dispose();

            _redis.Dispose();
        }

        public static void RemoveFromProcessingQueue(
            IRedisClient redis, string jobId, string serverName, string queueName)
        {
            redis.RemoveItemFromList(
                String.Format("hangfire:processing:{0}:{1}", serverName, queueName),
                jobId,
                -1);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
        private void Work()
        {
            try
            {
                AnnounceServer();

                _logger.Info("Starting to requeue processing jobs...");

                var requeued = RequeueProcessingJobs();

                _logger.Info(String.Format("Requeued {0} jobs.", requeued));

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
                        jobId = DequeueJobId(TimeSpan.FromSeconds(5));
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

        private string DequeueJobId(TimeSpan? timeOut)
        {
            return _redis.BlockingPopAndPushItemBetweenLists(
                    String.Format("hangfire:queue:{0}", _queueName),
                    String.Format("hangfire:processing:{0}:{1}", _serverName, _queueName),
                    timeOut);
        }

        private int RequeueProcessingJobs()
        {
            var queues = _redis.GetAllItemsFromSet(
                String.Format("hangfire:server:{0}:queues", _serverName));

            int requeued = 0;

            foreach (var queue in queues)
            {
                while (_redis.PopAndPushItemBetweenLists(
                    String.Format("hangfire:processing:{0}:{1}", _serverName, queue),
                    String.Format("hangfire:queue:{0}", queue)) != null)
                {
                    requeued++;
                    if (_cts.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }

            if (!_cts.IsCancellationRequested)
            {
                // TODO: one server - one queue. What is this?
                using (var transaction = _redis.CreateTransaction())
                {
                    transaction.QueueCommand(x => x.RemoveEntry(
                        String.Format("hangfire:server:{0}:queues", _serverName)));
                    transaction.QueueCommand(x => x.AddItemToSet(
                        String.Format("hangfire:server:{0}:queues", _serverName), _queueName));
                    transaction.Commit();
                }
            }

            return requeued;
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
                            { "server-name", _serverName },
                            { "concurrency", _concurrency.ToString() },
                            { "queue", _queueName },
                            { "started-at", JobHelper.ToStringTimestamp(DateTime.UtcNow) }
                        }));
                transaction.QueueCommand(x => x.AddItemToSet(
                    String.Format("hangfire:queue:{0}:servers", _queueName), _serverName));

                transaction.Commit();
            }
        }

        private void HideServer()
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromSet(
                    "hangfire:servers", _serverName));
                transaction.QueueCommand(x => x.RemoveEntry(
                    String.Format("hangfire:server:{0}", _serverName)));
                transaction.QueueCommand(x => x.RemoveItemFromSet(
                    String.Format("hangfire:queue:{0}:servers", _queueName), _serverName));

                transaction.Commit();
            }
        }
    }
}