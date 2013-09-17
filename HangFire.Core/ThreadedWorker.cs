using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace HangFire
{
    internal class ThreadedWorker : Worker, IDisposable
    {
        private readonly ThreadedWorkerManager _pool;
        private readonly Thread _thread;

        private readonly ManualResetEventSlim _jobIsReady
            = new ManualResetEventSlim(false);

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly object _crashedLock = new object();
        private readonly object _jobLock = new object();
        private bool _crashed;
        private bool _started;
        private bool _disposed;

        private string _jobId;

        public ThreadedWorker(
            ThreadedWorkerManager pool,
            WorkerContext workerContext,
            JobInvoker jobInvoker, HangFireJobActivator jobActivator)
            : base(workerContext, jobInvoker, jobActivator)
        {
            _pool = pool;

            _thread = new Thread(DoWork)
                {
                    Name = String.Format("HangFire.Worker.{0}", workerContext.WorkerNumber),
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

        public override void Process(string jobId)
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
                        base.Process(_jobId);

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
    }
}
