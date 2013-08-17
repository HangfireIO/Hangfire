using System;
using System.Threading;
using ServiceStack.Logging;

namespace HangFire
{
    internal class JobDispatcher : IDisposable
    {
        private readonly JobDispatcherPool _pool;
        private readonly string _name;

        private readonly JobProcessor _processor = new JobProcessor(
            Configuration.Instance.WorkerActivator,
            Configuration.Instance.PerformInterceptors);

        private readonly Thread _thread;

        private readonly ManualResetEventSlim _jobIsReady
            = new ManualResetEventSlim(false);

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly object _crashedLock = new object();
        private readonly object _jobLock = new object();
        private bool _crashed;
        private bool _started;
        private bool _disposed;

        private readonly ILog _logger;

        private volatile string _currentJob;

        public JobDispatcher(JobDispatcherPool pool, string name)
        {
            _logger = LogManager.GetLogger(name);
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
            if (_disposed) throw new ObjectDisposedException(GetType().Name);

            if (_started)
            {
                throw new InvalidOperationException("Dispatcher has been already started.");
            }

            _thread.Start();
            _started = true;
        }

        public void Stop()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);

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

        public void Process(string serializedJob)
        {
            if (_disposed) throw new InvalidOperationException(GetType().Name);

            lock (_jobLock)
            {
                _currentJob = serializedJob;
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
                        try
                        {
                            _processor.ProcessJob(_currentJob);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(
                                "Failed to process the job: unexpected exception caught. Job JSON:"
                                + Environment.NewLine
                                + _currentJob,
                                ex);

                        }
                        finally
                        {
                            _jobIsReady.Reset();
                        }

                        _pool.NotifyCompleted(_currentJob);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Crashed = true;
                _logger.Fatal(
                    String.Format("Unexpected exception caught in the job dispatcher '{0}'. It will be stopped.", _name),
                    ex);
            }
        }
    }
}
