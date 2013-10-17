using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using HangFire.Filters;
using HangFire.States;
using ServiceStack.Logging;
using ServiceStack.Redis;

namespace HangFire.Server
{
    internal class Worker : IDisposable
    {
        private readonly JobManager _manager;
        private readonly WorkerContext _context;
        private readonly Thread _thread;

        public static readonly IRedisClient Redis = RedisFactory.Create();
        protected readonly ILog Logger;

        private readonly ManualResetEventSlim _jobIsReady
            = new ManualResetEventSlim(false);

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly object _crashedLock = new object();
        private readonly object _jobLock = new object();
        private bool _crashed;
        private bool _stopSent;

        private JobPayload _jobPayload;

        public Worker(JobManager manager, WorkerContext context)
        {
            _manager = manager;
            _context = context;

            Logger = LogManager.GetLogger(String.Format("HangFire.Worker.{0}", _context.WorkerNumber));

            _thread = new Thread(DoWork)
                {
                    Name = String.Format("HangFire.Worker.{0}", _context.WorkerNumber),
                    IsBackground = true
                };
            _thread.Start();
        }

        public void SendStop()
        {
            _stopSent = true;
            _cts.Cancel();
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

        public void Process(JobPayload payload)
        {
            lock (_jobLock)
            {
                _jobPayload = payload;
            }

            _jobIsReady.Set();
        }

        public void Dispose()
        {
            if (!_stopSent)
            {
                SendStop();
            }

            _thread.Join();

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
                    _manager.NotifyReady(this);
                    _jobIsReady.Wait(_cts.Token);

                    lock (_jobLock)
                    {
                        PerformJob(_jobPayload);
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
        private void PerformJob(JobPayload payload)
        {
            if (String.IsNullOrEmpty(payload.Type))
            {
                Logger.Warn(String.Format(
                    "Could not process the job '{0}': it does not exist in the storage.",
                    payload.Id));

                return;
            }

            lock (Redis)
            {
                if (!JobState.Apply(
                    Redis,
                    new ProcessingState(payload.Id, "Worker has started processing.", _context.ServerName),
                    EnqueuedState.Name))
                {
                    return;
                }
            }
            
            // Checkpoint #3. Job is in the Processing state. However, there are
            // no guarantees that it was performed. We need to re-queue it even
            // it was performed to guarantee that it was performed AT LEAST once.
            // It will be re-queued after the JobTimeout was expired.

            Exception exception = null;

            ServerJobDescriptor jobDescriptor = null;
            try
            {
                jobDescriptor = new ServerJobDescriptor(
                    _context.Activator, payload);

                var performContext = new PerformContext(
                    _context, jobDescriptor);

                _context.Performer.PerformJob(performContext);
            }
            catch (Exception ex)
            {
                exception = ex;

                Logger.Error(String.Format(
                    "Failed to process the job '{0}': unexpected exception caught.",
                    payload.Id));
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
                        new SucceededState(payload.Id, "The job has been completed successfully."),
                        ProcessingState.Name);
                }
                else
                {
                    JobState.Apply(
                        Redis,
                        new FailedState(payload.Id, "The job has been failed.", exception),
                        ProcessingState.Name);
                }

                // Checkpoint #4. The job was performed, and it is in the one
                // of the explicit states (Succeeded, Scheduled and so on).
                // It should not be re-queued, but we still need to remove it's
                // processing information.

                JobFetcher.RemoveFromFetchedQueue(
                    Redis, payload.Id, payload.Queue);

                // Success point. No things must be done after previous command
                // was succeeded.
            }
        }
    }
}
