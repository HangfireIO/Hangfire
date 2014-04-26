using System;
using System.Threading;
using Common.Logging;

namespace HangFire.Server
{
    internal class ServerComponentRunner : IServerComponentRunner
    {
        private readonly IServerComponent _component;
        private readonly ServerComponentRunnerOptions _options;
        private readonly Thread _thread;
        private readonly ILog _logger;

        private readonly ManualResetEventSlim _starting = new ManualResetEventSlim(false);
        private readonly CancellationTokenSource _disposingCts = new CancellationTokenSource();
        private bool _disposed;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly object _ctsLock = new object();
        

        public ServerComponentRunner(IServerComponent component)
            : this(component, new ServerComponentRunnerOptions())
        {
        }

        public ServerComponentRunner(IServerComponent component, ServerComponentRunnerOptions options)
        {
            if (component == null) throw new ArgumentNullException("component");
            if (options == null) throw new ArgumentNullException("options");

            _component = component;
            _options = options;

            _logger = LogManager.GetLogger(_component.GetType());
            _thread = new Thread(RunComponent) { IsBackground = true };

            _logger.TraceFormat("Starting a new thread for server component '{0}'...", _component);
            _thread.Start();
        }

        public void Start()
        {
            CheckDisposed();

            _logger.TraceFormat("Starting server component '{0}'...", _component);

            _starting.Set();
        }

        public void Stop()
        {
            CheckDisposed();

            _logger.TraceFormat("Stopping server component '{0}'...", _component);

            _starting.Reset();

            lock (_ctsLock)
            {
                _cts.Cancel();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _logger.TraceFormat("Initiating shutdown for server component '{0}'...", _component);

            Stop();

            _disposed = true;
            _disposingCts.Cancel();

            _thread.Join(_options.ShutdownTimeout);

            _disposingCts.Dispose();
            _starting.Dispose();
            _cts.Dispose();
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private void RunComponent()
        {
            try
            {
                try
                {
                    while (true)
                    {
                        // While a runner thread is being in this loop,
                        // the runner can be started, stopped, restarted, etc.

                        _starting.Wait(_disposingCts.Token);

                        LogComponentStarted();

                        ExecuteComponent();

                        LogComponentStopped();
                    }
                }
                finally
                {
// ReSharper disable once SuspiciousTypeConversion.Global
                    var disposable = _component as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.TraceFormat("Server component '{0}' disposed.", _component);
            }
            catch (ThreadAbortException)
            {
                _logger.WarnFormat(
                    "Server component '{0}' caught `ThreadAbortException` due to shutdown timeout.",
                    _component);
            }
            catch (Exception ex)
            {
                _logger.FatalFormat(
                    "Fatal error occured during execution of '{0}' component. It will be stopped. See the exception for details.",
                    ex,
                    _component);
            }
        }

        private void ExecuteComponent()
        {
            try
            {
                // Each component encapsulates one loop iteration, so
                // the real infinite loop is going here.
                while (true)
                {
                    // There is no guarantee that component will throw
                    // canceled exception, so we need to do it manually.
                    _cts.Token.ThrowIfCancellationRequested();

                    // This is background component and it should
                    // repair on transient exceptions automatically.
                    ExecuteWithAutomaticRetry(_cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                lock (_ctsLock)
                {
                    _cts.Dispose();
                    _cts = new CancellationTokenSource();
                }
            }
        }

        private void ExecuteWithAutomaticRetry(CancellationToken cancellationToken)
        {
            for (var i = 0; i <= _options.MaxRetryAttempts; i++)
            {
                try
                {
                    _component.Execute(cancellationToken);
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // Break the loop after the retry attempts number exceeded.
                    if (i >= _options.MaxRetryAttempts - 1) throw;

                    // Break the loop when the wait handle was signaled.
                    if (SleepBackOffMultiplier(i, cancellationToken.WaitHandle))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }
        }

        private static bool SleepBackOffMultiplier(int i, WaitHandle waitHandle)
        {
            //exponential/random retry back-off.
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var nextTry = rand.Next(
                (int)Math.Pow(i, 2), (int)Math.Pow(i + 1, 2) + 1);

            return waitHandle.WaitOne(TimeSpan.FromSeconds(nextTry));
        }

        private void LogComponentStarted()
        {
            const string startedMessage = "Server component '{0}' started.";
            if (_options.MinimumLogVerbosity)
            {
                _logger.DebugFormat(startedMessage, _component);
            }
            else
            {
                _logger.InfoFormat(startedMessage, _component);
            }
        }

        private void LogComponentStopped()
        {
            const string stoppedMessage = "Server component '{0}' stopped.";
            if (_options.MinimumLogVerbosity)
            {
                _logger.DebugFormat(stoppedMessage, _component);
            }
            else
            {
                _logger.InfoFormat(stoppedMessage, _component);
            }
        }
    }
}