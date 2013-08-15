using System;
using System.Threading;
using ServiceStack.Logging;

namespace HangFire
{
    internal class JobDispatcher
    {
        private readonly JobDispatcherPool _pool;
        private readonly string _name;

        private readonly JobProcessor _processor = new JobProcessor(Configuration.Instance.WorkerActivator);
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _jobIsReady 
            = new ManualResetEventSlim(false);

        private readonly object _errorLock = new object();
        private bool _isError;

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
            _thread.Start();
        }

        public bool IsStopped
        {
            get
            {
                lock (_errorLock)
                {
                    return _isError;
                }
            }
            private set
            {
                lock (_errorLock)
                {
                    _isError = value;
                }
            }
        }

        public void Process(string serializedJob)
        {
            _currentJob = serializedJob;
            _jobIsReady.Set();
        }

        private void DoWork()
        {
            try
            {
                while (true)
                {
                    _pool.NotifyReady(this);
                    _jobIsReady.Wait();

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
                        _pool.NotifyFailed(_currentJob, ex);

                    }
                    finally
                    {
                        _jobIsReady.Reset();
                    }
                }
            }
            catch (Exception ex)
            {
                IsStopped = true;
                _logger.Fatal(
                    String.Format("Unexpected exception caught in the job dispatcher '{0}'. It will be stopped.", _name), 
                    ex);
            }
        }
    }
}
