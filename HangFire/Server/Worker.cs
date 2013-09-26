using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using HangFire.Storage;
using ServiceStack.Logging;

namespace HangFire.Server
{
    internal class Worker : IDisposable
    {
        private readonly WorkerPool _pool;
        private readonly ServerContext _serverContext;
        private readonly int _workerNumber;
        private readonly ServerJobInvoker _jobInvoker;
        private readonly JobActivator _jobActivator;
        private readonly Thread _thread;

        public static readonly RedisStorage Redis = new RedisStorage();
        protected readonly ILog Logger;

        private readonly ManualResetEventSlim _jobIsReady
            = new ManualResetEventSlim(false);

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly object _crashedLock = new object();
        private readonly object _jobLock = new object();
        private bool _crashed;
        private bool _started;
        private bool _disposed;

        private string _jobId;

        public Worker(
            WorkerPool pool,
            ServerContext serverContext,
            int workerNumber,
            ServerJobInvoker jobInvoker, JobActivator jobActivator)
        {
            _pool = pool;
            _serverContext = serverContext;
            _workerNumber = workerNumber;
            _jobInvoker = jobInvoker;
            _jobActivator = jobActivator;

            Logger = LogManager.GetLogger(String.Format("HangFire.Worker.{0}", workerNumber));

            _thread = new Thread(DoWork)
                {
                    Name = String.Format("HangFire.Worker.{0}", workerNumber),
                    IsBackground = true
                };
        }

        public void Start()
        {
            Debug.Assert(!_disposed, "!_disposed");

            if (_started)
            {
                throw new InvalidOperationException("Worker has been already started.");
            }

            _thread.Start();
            _started = true;
        }

        public void Stop()
        {
            Debug.Assert(!_disposed, "!_disposed");

            if (_started)
            {
                _cts.Cancel();
            }
        }

        internal bool Crashed
        {
            get
            {
                lock (_crashedLock)
                {
                    return _crashed;
                }
            }
            private set
            {
                lock (_crashedLock)
                {
                    _crashed = value;
                }
            }
        }

        public void Process(string jobId)
        {
            Debug.Assert(!_disposed, "!_disposed");

            lock (_jobLock)
            {
                _jobId = jobId;
            }

            _jobIsReady.Set();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_started)
            {
                _thread.Join();
            }

            _cts.Dispose();
            _jobIsReady.Dispose();
        }

        private void DoWork()
        {
            try
            {
                while (true)
                {
                    _pool.NotifyReady(this);
                    _jobIsReady.Wait(_cts.Token);

                    lock (_jobLock)
                    {
                        PerformJob(_jobId);

                        _pool.NotifyCompleted(_jobId);
                        _jobIsReady.Reset();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Crashed = true;
                Logger.Fatal(
                    String.Format(
                        "Unexpected exception caught. The worker will be stopped."),
                    ex);
            }
        }

        private void PerformJob(string jobId)
        {
            string jobType = null;
            Dictionary<string, string> jobArgs = null;

            try
            {
                lock (Redis)
                {
                    Redis.RetryOnRedisException(
                        x => x.GetJobTypeAndArgs(jobId, out jobType, out jobArgs),
                        _cts.Token);
                }

                if (String.IsNullOrEmpty(jobType))
                {
                    Logger.Warn(String.Format(
                        "Could not process the job '{0}': it does not exist in the storage.",
                        jobId));

                    return;
                }

                var workerContext = new WorkerContext(_serverContext, _workerNumber, Redis);

                lock (Redis)
                {
                    Redis.RetryOnRedisException(
                        x => x.AddProcessingWorker(workerContext.ServerContext.ServerName, jobId),
                        _cts.Token);
                }

                Exception exception = null;

                ServerJobDescriptor jobDescriptor = null;
                try
                {
                    jobDescriptor = new ServerJobDescriptor(_jobActivator, jobId, jobType, jobArgs);
                    _jobInvoker.PerformJob(workerContext, jobDescriptor);
                }
                catch (Exception ex)
                {
                    exception = ex;

                    Logger.Error(String.Format(
                        "Failed to process the job '{0}': unexpected exception caught.",
                        jobId));
                }
                finally
                {
                    if (jobDescriptor != null)
                    {
                        jobDescriptor.Dispose();
                    }
                }

                lock (Redis)
                {
                    Redis.RetryOnRedisException(
                        x => x.RemoveProcessingWorker(jobId, exception),
                        _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
