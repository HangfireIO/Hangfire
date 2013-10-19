using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class JobManager
    {
        private readonly List<Worker> _workers;
        private readonly BlockingCollection<Worker> _freeWorkers;

        private readonly Thread _managerThread;
        private readonly IJobFetcher _fetcher;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ILog _logger = LogManager.GetLogger(typeof (JobManager));

        private bool _stopSent;
        
        public JobManager(
            IRedisClientsManager redisManager,
            ServerContext context, 
            int workerCount, 
            IEnumerable<string> queues)
        {
            _workers = new List<Worker>(workerCount);
            _freeWorkers = new BlockingCollection<Worker>();

            _logger.Info(String.Format("Starting {0} workers...", workerCount));

            for (var i = 0; i < workerCount; i++)
            {
                _workers.Add(
                    new Worker(redisManager, this, new WorkerContext(context, i)));
            }

            _logger.Info("Workers were started.");

            _fetcher = new PrioritizedJobFetcher(
                redisManager, queues, workerCount);

            _managerThread = new Thread(Work)
                {
                    Name = typeof(JobManager).Name,
                    IsBackground = true
                };
            _managerThread.Start();
        }

        public void SendStop()
        {
            _stopSent = true;

            _cts.Cancel();

            foreach (var worker in _workers)
            {
                worker.SendStop();
            }
        }

        public void Dispose()
        {
            if (!_stopSent)
            {
                SendStop();
            }

            _managerThread.Join();

            foreach (var worker in _workers)
            {
                worker.Dispose();
            }
            _logger.Info("Workers were stopped.");

            _fetcher.Dispose();

            _freeWorkers.Dispose();
            _cts.Dispose();
        }

        internal void NotifyReady(Worker worker)
        {
            _freeWorkers.Add(worker);
        }

        private void Work()
        {
            try
            {
                while (true)
                {
                    Worker worker;
                    do
                    {
                        worker = _freeWorkers.Take(_cts.Token);
                    }
                    while (worker.Crashed);

                    var jobId = _fetcher.DequeueJob(_cts.Token);
                    worker.Process(jobId);
                }
            }
            catch (OperationCanceledException)
            {
                // TODO: log it
            }
            catch (Exception ex)
            {
                _logger.Fatal(
                    String.Format(
                        "Unexpected exception caught. Jobs  will not be processed by this server."),
                    ex);
            }
        }
    }
}
