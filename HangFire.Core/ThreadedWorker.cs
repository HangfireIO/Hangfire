using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace HangFire
{
    internal class ThreadedWorker : Worker, IDisposable
    {
        private readonly ThreadedWorkerManager _pool;
        private readonly string _name;

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

        public ThreadedWorker(ThreadedWorkerManager pool, string name, string workerName, HangFireJobActivator activator)
            : base(name, workerName, activator)
        {
            _pool = pool;
            _name = name;

            _thread = new Thread(DoWork)
                {
                    Name = name,
                    IsBackground = true
                };
        }

        public void Start()
        {
            Debug.Assert(!_disposed, "!_disposed");

            if (_started)
            {
                throw new InvalidOperationException("Dispatcher has been already started.");
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
                    String.Format("Unexpected exception caught in the job dispatcher '{0}'. It will be stopped.", _name),
                    ex);
            }
        }
    }
}
