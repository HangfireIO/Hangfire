using System;
using System.Collections.Concurrent;
using System.Threading;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class WorkerManager : IThreadWrappable, IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(WorkerManager));

        private readonly DisposableCollection<Worker> _workers;
        private readonly BlockingCollection<Worker> _freeWorkers;

        private readonly IJobFetcher _fetcher;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        
        public WorkerManager(
            IJobFetcher fetcher,
            IRedisClientsManager redisManager,
            ServerContext context,
            int workerCount)
        {
            _freeWorkers = new BlockingCollection<Worker>();
            _workers = new DisposableCollection<Worker>();

            for (var i = 1; i <= workerCount; i++)
            {
                var workerContext = new WorkerContext(context, i);

                var worker = new Worker(this, redisManager, workerContext);
                worker.Start();

                _workers.Add(worker);
            }

            _fetcher = fetcher;
        }

        public void Dispose()
        {
            Logger.Info("Stopping workers...");

            _workers.Dispose();

            Logger.Info("Workers were stopped.");

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
                Logger.InfoFormat("Worker manager has been started with {0} workers.", _workers.Count);

                while (true)
                {
                    ProcessNextJob(_cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Info("Worker manager has been stopped.");
            }
            catch (Exception ex)
            {
                Logger.Fatal(
                    String.Format(
                        "Unexpected exception caught. Jobs  will not be processed by this server."),
                    ex);
            }
        }
    }
}
