using System;
using System.Collections.Concurrent;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class WorkerManager : IThreadWrappable, IDisposable
    {
        private readonly DisposableCollection<Worker> _workers;
        private readonly BlockingCollection<Worker> _freeWorkers;

        private readonly IJobFetcher _fetcher;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ILog _logger = LogManager.GetLogger(typeof (WorkerManager));
        
        public WorkerManager(
            IJobFetcher fetcher,
            IRedisClientsManager redisManager,
            ServerContext context,
            int workerCount)
        {
            _freeWorkers = new BlockingCollection<Worker>();

            _logger.Info(String.Format("Starting {0} workers...", workerCount));

            _workers = new DisposableCollection<Worker>();

            for (var i = 0; i < workerCount; i++)
            {
                var workerContext = new WorkerContext(context, i);

                var worker = new Worker(this, redisManager, workerContext);
                worker.Start();

                _workers.Add(worker);
            }

            _logger.Info("Workers were started.");

            _fetcher = fetcher;
        }

        public void Dispose()
        {
            _workers.Dispose();

            _logger.Info("Workers were stopped.");

            _fetcher.Dispose();

            _freeWorkers.Dispose();
            _cts.Dispose();
        }

        public void ProcessNextJob(CancellationToken cancellationToken)
        {
            Worker worker;
            do
            {
                worker = _freeWorkers.Take(cancellationToken);
            }
            while (worker.Crashed);

            var jobId = _fetcher.DequeueJob(cancellationToken);
            worker.Process(jobId);
        }

        internal void NotifyReady(Worker worker)
        {
            _freeWorkers.Add(worker);
        }

        void IThreadWrappable.Dispose(Thread thread)
        {
            _cts.Cancel();
            thread.Join();
        }

        void IThreadWrappable.Work()
        {
            try
            {
                while (true)
                {
                    ProcessNextJob(_cts.Token);
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
