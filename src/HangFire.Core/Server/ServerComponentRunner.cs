// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

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
            _thread = new Thread(RunComponent) { IsBackground = true, Name = component.ToString() };

            _logger.TraceFormat("Starting a new thread for server component '{0}'...", _component);
            _thread.Start();
        }

        public IServerComponent Component
        {
            get { return _component; }
        }

        public void Start()
        {
            CheckDisposed();

            _logger.TraceFormat("Sending start request for server component '{0}'...", _component);

            _starting.Set();
        }

        public void Stop()
        {
            CheckDisposed();

            _starting.Reset();

            _logger.TraceFormat("Sending stop request for server component '{0}'...", _component);

            lock (_ctsLock)
            {
                _cts.Cancel();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            Stop();

            _disposed = true;

            _logger.TraceFormat("Sending shutdown request for server component '{0}'...", _component);

            _disposingCts.Cancel();
            if (!_thread.Join(_options.ShutdownTimeout))
            {
                _thread.Abort();
            }

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
                _logger.DebugFormat("Starting server component '{0}'...", _component);

                try
                {
                    while (true)
                    {
                        // While a runner thread is being in this loop,
                        // the runner can be started, stopped, restarted, etc.

                        _starting.Wait(_disposingCts.Token);

                        const string message = "Server component '{0}' started.";
                        if (_options.LowerLogVerbosity)
                        {
                            _logger.DebugFormat(message, _component);
                        }
                        else
                        {
                            _logger.InfoFormat(message, _component);
                        }

                        ExecuteComponent();

                        _logger.DebugFormat("Stopping server component '{0}'...", _component);
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
                const string message = "Server component '{0}' stopped.";
                if (_options.LowerLogVerbosity)
                {
                    _logger.DebugFormat(message, _component);
                }
                else
                {
                    _logger.InfoFormat(message, _component);
                }
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
                    "Fatal error occurred during execution of '{0}' component. It will be stopped. See the exception for details.",
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
                catch (Exception ex)
                {
                    // Break the loop after the retry attempts number exceeded.
                    if (i >= _options.MaxRetryAttempts - 1) throw;

                    var nextTry = GetBackOffMultiplier(i);

                    _logger.ErrorFormat(
                        "Error occurred during execution of '{0}' component. Execution will be retried (attempt {1} of {2}) in {3} seconds.",
                        ex,
                        _component,
                        i + 1,
                        _options.MaxRetryAttempts,
                        nextTry);

                    // Break the loop when the wait handle was signaled.
                    cancellationToken.WaitHandle.WaitOne(nextTry);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        private static TimeSpan GetBackOffMultiplier(int retryAttemptNumber)
        {
            //exponential/random retry back-off.
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var nextTry = rand.Next(
                (int)Math.Pow(retryAttemptNumber, 2), (int)Math.Pow(retryAttemptNumber + 1, 2) + 1);

            return TimeSpan.FromSeconds(nextTry);
        }
    }
}