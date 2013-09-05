using System;
using System.Diagnostics;
using System.Threading;
using ServiceStack.Logging;

namespace HangFire
{
    internal class Worker : IDisposable
    {
        private static readonly RedisClient Client = new RedisClient();

        private readonly WorkerManager _pool;
        private readonly string _name;
        private readonly string _name2;

        private readonly JobInvoker _invoker;

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

        public Worker(WorkerManager pool, string name, string name2, HangFireJobActivator jobActivator)
        {
            _logger = LogManager.GetLogger(name);
            _pool = pool;
            _name = name;
            _name2 = name2;

            _invoker = new JobInvoker(
                jobActivator,
                HangFireConfiguration.Current.ServerFilters);

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

        public void Process(string serializedJob)
        {
            Debug.Assert(!_disposed, "!_disposed");

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
                        // TODO: Handle deserialization errors.
                        var job = JobDescription.Deserialize(_currentJob);

                        lock (Client)
                        {
                            // Пока нет связи с Redis, бессмысленно что-то начинать делать,
                            // поэтому будем повторять действие до тех пор, пока не 
                            // восстановится связь.
                            while (!Client.TryToDo(x => x.AddProcessingDispatcher(
                                _name2, job.WorkerType.Name, job.SerializeArgs())))
                            {
                            }
                        }

                        Exception exception = null;

                        try
                        {
                            _invoker.ProcessJob(job);
                        }
                        catch (Exception ex)
                        {
                            exception = ex;

                            _logger.Error(
                                "Failed to process the job: unexpected exception caught. Job JSON:"
                                + Environment.NewLine
                                + _currentJob,
                                ex);
                        }

                        var now = DateTime.UtcNow;
                        if (exception == null)
                        {
                            job.SucceededAt = now;
                        }
                        else
                        {
                            job.FailedAt = now;
                            job.Properties["ExceptionType"] = exception.GetType().FullName;
                            job.Properties["ExceptionMessage"] = exception.Message;
                            job.Properties["StackTrace"] = exception.StackTrace;
                        }

                        // TODO: Handle Redis exceptions.
                        lock (Client)
                        {
                            // См. комментарий к подобному блоку выше.
                            while (!Client.TryToDo(x => x.RemoveProcessingDispatcher(_name2, job, exception)))
                            {
                            }
                        }

                        // We need unmodified job here.
                        _pool.NotifyCompleted(_currentJob);

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
                _logger.Fatal(
                    String.Format("Unexpected exception caught in the job dispatcher '{0}'. It will be stopped.", _name),
                    ex);
            }
        }
    }
}
