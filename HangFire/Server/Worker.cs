using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using HangFire.States;
using ServiceStack.Logging;
using ServiceStack.Redis;

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

        public static readonly IRedisClient Redis = RedisFactory.Create();
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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Unexpected exception should not fail the whole application.")]
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

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We need to catch all user-code exceptions.")]
        private void PerformJob(string jobId)
        {
            try
            {
                Dictionary<string, string> jobArgs;
                string jobType;

                GetJobTypeAndArgs(jobId, out jobType, out jobArgs);

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
                    if (!JobState.Apply(
                        Redis, 
                        new ProcessingState(jobId, "Worker has started processing.", workerContext.ServerContext.ServerName),
                        EnqueuedState.Name,
                        ProcessingState.Name))
                    {
                        return;
                    }
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
                    if (exception == null)
                    {
                        JobState.Apply(
                            Redis, 
                            new SucceededState(jobId, "The job has been completed successfully."),
                            ProcessingState.Name);
                    }
                    else
                    {
                        JobState.Apply(
                            Redis, 
                            new FailedState(jobId, "The job has been failed.", exception),
                            ProcessingState.Name);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static void GetJobTypeAndArgs(string jobId, out string jobType, out Dictionary<string, string> jobArgs)
        {
            lock (Redis)
            {
                var result = Redis.GetValuesFromHash(
                    String.Format("hangfire:job:{0}", jobId),
                    new[] { "Type", "Args" });

                jobType = result[0];
                jobArgs = JobHelper.FromJson<Dictionary<string, string>>(result[1]);
            }
        }
    }
}
