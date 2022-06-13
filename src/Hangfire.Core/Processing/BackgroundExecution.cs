// This file is part of Hangfire. Copyright © 2017 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Logging;
using ThreadState = System.Threading.ThreadState;

namespace Hangfire.Processing
{
    internal sealed class BackgroundExecution : IBackgroundExecution
    {
        // This fallback strategy is used for defensive purposes, when there are
        // problems with obtaining retry delays we should not under any circumstances
        // fall into constant immediate retries, consuming 100% of CPU.
        private static readonly TimeSpan FallbackRetryDelay = TimeSpan.FromSeconds(5);

        // Execution can be in one of three states: Running, Faulted or Failed. Each
        // one defines its own logging rules to lower the number of logged messages,
        // to not to make stress on logging subsystem with thousands of messages in
        // case of transient faults.
        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);
        private Stopwatch _faultedSince;
        private Stopwatch _failedSince;
        private Stopwatch _lastException;
        private int _exceptionsCount;

        private CancellationToken _stopToken;
        private readonly BackgroundExecutionOptions _options;
        private readonly ILog _logger;

        private readonly Stopwatch _createdAt;
        private Stopwatch _stoppedAt;
        private CancellationTokenRegistration _stopRegistration;

        private volatile bool _disposed;

        public BackgroundExecution(CancellationToken stopToken, [NotNull] BackgroundExecutionOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _logger = LogProvider.GetLogger(GetType());
            _createdAt = Stopwatch.StartNew();

            _stopToken = stopToken;

            _stopRegistration = _stopToken.Register(SetStoppedAt);

#if !NETSTANDARD1_3
            AppDomainUnloadMonitor.EnsureInitialized();
#endif
        }

        public bool StopRequested => _disposed || _stopToken.IsCancellationRequested;

        public void Run(Action<Guid, object> callback, object state)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var executionId = Guid.NewGuid();

            // ExecutionId is a custom correlation id for logging purposes. We can use Thread's
            // ManagedThreadId property here, but it's better to have a single implementation between
            // sync and async dispatchers - async one can execute related tasks on different threads,
            // so ManagedThreadId doesn't work there.
            //using (LogProvider.OpenMappedContext("ExecutionId", executionId.ToString()))
            {
#if !NETSTANDARD1_3
                try
#endif
                {
                    HandleStarted(executionId, out var nextDelay);

                    // There should be no operations between the `while` and `try` blocks to
                    // avoid unintended stopping due to ThreadAbortException between the loop
                    // iterations. Even loop condition is placed into the `try` block.
                    while (true)
                    {
                        // Don't place anything here.
                        try
                        {
                            // All possible exceptions should be handled inside this try/catch
                            // block, including ThreadAbortException (ResetAbort is called when
                            // possible) and ThreadInterruptedException. The loop could only be
                            // interrupted by the corresponding cancellation token, or thread
                            // abort, caused by appdomain unload.

                            // Don't use ThrowIfCancellationRequested here, because it may cause
                            // infinite looping when ThreadAbortException is raised during app
                            // domain unloads.
                            if (StopRequested) break;

                            if (nextDelay != TimeSpan.Zero)
                            {
                                if (!HandleDelay(executionId, nextDelay))
                                {
                                    // Inability to handle the delay means that execution was
                                    // already stopped, so we should break the loop.
                                    break;
                                }
                            }

                            callback(executionId, state);
                            HandleSuccess(out nextDelay);
                        }
#if !NETSTANDARD1_3
                        catch (ThreadAbortException) when (AppDomainUnloadMonitor.IsUnloading)
                        {
                            // Our thread is aborted due to AppDomain unload. It's better to give up to
                            // not to cause the host to be more aggressive.
                            throw;
                        }
#endif
                        catch (OperationCanceledException ex) when (ex.CancellationToken.Equals(_stopToken) || StopRequested)
                        {
                            // We are catching general OCE exception without checking its CancellationToken
                            // property, because the concrete token may be different than our one, for
                            // example when using linked token sources. When we get OCE and our stop token
                            // is canceled, we can simply break the execution loop, because it will be
                            // broken on next iteration anyway.
                            break;
                        }
                        catch (Exception ex) when (ex.IsCatchableExceptionType())
                        {
                            HandleException(executionId, ex, out nextDelay);
                        }
                    }

                    HandleStop(executionId);
                }
#if !NETSTANDARD1_3
                catch (ThreadAbortException ex)
                {
                    // This is a rude stop. Since we are handling all the thread aborts
                    // inside the loop, only appdomain unload can bring us there. In this
                    // case we don't reset thread abort.
                    HandleThreadAbort(executionId, ex);
                }
#endif
            }
        }

        public async Task RunAsync(Func<Guid, object, Task> callback, object state)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var executionId = Guid.NewGuid();

            // We can't use Thread.ManagedThreadId to provide a correlation id for logging subsystem,
            // because task and its continuations can run in different threads due to work stealing
            // nature of fetching. So instead we are using a custom identifier, hoping OpenMappedContext
            // implementation uses AsyncLocal instead of ThreadLocal ;-)
            //using (LogProvider.OpenMappedContext("ExecutionId", executionId.ToString()))
            {
#if !NETSTANDARD1_3
                try
#endif
                {
                    HandleStarted(executionId, out var nextDelay);

                    // There should be no operations between the `while` and `try` blocks to
                    // avoid unintended stopping due to ThreadAbortException between the loop
                    // iterations. Even loop condition is placed into the `try` block.
                    while (true)
                    {
                        // Don't place anything here.
                        try
                        {
                            // All possible exceptions should be handled inside this try/catch
                            // block, including ThreadAbortException (ResetAbort is called when
                            // possible) and ThreadInterruptedException. The loop could only be
                            // interrupted by the corresponding cancellation token, or thread
                            // abort, caused by appdomain unload.

                            // Don't use ThrowIfCancellationRequested here, because it may cause
                            // infinite looping when ThreadAbortException is raised during app
                            // domain unloads.
                            if (StopRequested) break;

                            if (nextDelay != TimeSpan.Zero)
                            {
                                if (!await HandleDelayAsync(executionId, nextDelay).ConfigureAwait(true))
                                {
                                    // Inability to handle the delay means that execution was
                                    // already stopped, so we should break the loop.
                                    break;
                                }
                            }

                            await callback(executionId, state).ConfigureAwait(true);
                            HandleSuccess(out nextDelay);
                        }
#if !NETSTANDARD1_3
                        catch (ThreadAbortException) when (AppDomainUnloadMonitor.IsUnloading)
                        {
                            // Our previous task was aborted due to AppDomain unload. It's better to
                            // give up to not to cause the host to be more aggressive.
                            throw;
                        }
#endif
                        catch (OperationCanceledException ex) when (ex.CancellationToken.Equals(_stopToken) || StopRequested)
                        {
                            // We are catching general OCE exception without checking its CancellationToken
                            // property, because the concrete token may be different than our one, for
                            // example when using linked token sources. When we get OCE and our stop token
                            // is canceled, we can simply break the execution loop, because it will be
                            // broken on next iteration anyway.
                            break;
                        }
                        catch (Exception ex) when (ex.IsCatchableExceptionType())
                        {
                            HandleException(executionId, ex, out nextDelay);
                        }
                    }

                    HandleStop(executionId);
                }
#if !NETSTANDARD1_3
                catch (ThreadAbortException ex)
                {
                    // This is a rude stop. Since we are handling all the thread aborts
                    // inside the loop, only appdomain unload can bring us there. In this
                    // case we don't reset thread abort.
                    HandleThreadAbort(executionId, ex);
                }
#endif
            }
        }

        public void Dispose()
        {
            lock (_stopped)
            {
                if (_disposed) return;
                _disposed = true;

                _stopRegistration.Dispose();

                _stopped.Set();
                _stopped.Dispose();
            }
        }

        public void NotifySucceeded()
        {
            if (StopRequested) return;

            // This is an optimization to avoid lock acquisitions on every
            // loop iteration. It is possible that value will become null
            // upon entering the lock, but this race condition is benign.
            if (Volatile.Read(ref _faultedSince) != null)
            {
                ToRunningState();
            }
        }

        public override string ToString()
        {
            return _options?.Name ?? GetType().Name;
        }

        private void HandleStarted(Guid executionId, out TimeSpan initialDelay)
        {
            _logger.Debug($"{GetExecutionLoopTemplate(executionId)} has started in {_createdAt.Elapsed.TotalMilliseconds} ms");

            // Looks weird, but several times I was initializing the nextDelay variable
            // inside the execution loop by mistake. This lead to immediate looped invocation
            // with no delays and 100% of CPU consumption on transient exceptions, that is
            // completely unacceptable. So this is just a defensive technique.
            initialDelay = TimeSpan.Zero;
        }

        private bool HandleDelay(Guid executionId, TimeSpan delay)
        {
            try
            {
                LogRetry(executionId, delay);
                return !_stopped.WaitOne(delay, _stopToken);
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                LogUnableWait(executionId, delay, ex);
                return false;
            }
        }

        private async Task<bool> HandleDelayAsync(Guid executionId, TimeSpan delay)
        {
            try
            {
                LogRetry(executionId, delay);
                return !await _stopped.WaitOneAsync(delay, _stopToken).ConfigureAwait(true);
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                LogUnableWait(executionId, delay, ex);
                return false;
            }
        }

        private void LogUnableWait(Guid executionId, TimeSpan delay, Exception ex)
        {
            _logger.FatalException($"{GetExecutionLoopTemplate(executionId)} was unable to wait for '{delay}' delay due to an exception. Execution will be stopped.", ex);
        }

        private void LogRetry(Guid executionId, TimeSpan delay)
        {
            _logger.Debug($"{GetExecutionLoopTemplate(executionId)} will be retried in {delay}...");
        }

        private void NormalizeDelay(ref TimeSpan retryDelay)
        {
            if (retryDelay <= TimeSpan.Zero)
            {
                _logger.Warn($"{GetExecutionTemplate()} adjusted the retry delay from {retryDelay} to {FallbackRetryDelay}");
                retryDelay = FallbackRetryDelay;
            }
        }

        private void HandleSuccess(out TimeSpan nextDelay)
        {
            nextDelay = TimeSpan.Zero;
            NotifySucceeded();
        }

        private void HandleException(Guid executionId, Exception exception, out TimeSpan delay)
        {
#if !NETSTANDARD1_3
            // Normally, there should be no checks for AppDomain unload condition, because we can't
            // get here on appdomain unloads. But Mono < 5.4 has an issue with Thread.ResetAbort, and
            // it can prevent appdomain to be unloaded: https://bugzilla.xamarin.com/show_bug.cgi?id=5804.
            // It's better to reassure this can't happen under all circumstances.
            if ((Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) != 0 && !AppDomainUnloadMonitor.IsUnloading)
            {
                try
                {
                    Thread.ResetAbort();
                }
                catch (Exception ex) when (ex.IsCatchableExceptionType())
                {
                    // .NET Core doesn't support both Thread.Abort and Thread.ResetAbort methods.
                    // I don't see any possible cases, where thread is aborted, but nevertheless
                    // we shouldn't hide the original exception.
                    _logger.ErrorException($"{GetExecutionLoopTemplate(executionId)} was unable to reset thread abort request due to an exception. Background execution can be prematurely stopped.", ex);
                }
            }
#endif

            if (StopRequested)
            {
                delay = FallbackRetryDelay;
                return;
            }

            try
            {
                // Some code might cache exception object and throw the same instance
                // from multiple threads, despite it's not recommended to do. However
                // bad things happen, and we should have some diagnostic tools to
                // understand what's happened and what was the original exception which
                // is being modified.
                if (!exception.Data.Contains("ExecutionId"))
                {
                    exception.Data.Add("ExecutionId", executionId);
                }
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                _logger.WarnException($"Was unable to add the ExecutionId property to the exception object, please see inner exception for details. Original exception: ${exception.GetType()} (${exception.Message})", ex);
            }

            ToFailedState(exception, out delay);

            _logger.DebugException($"{GetExecutionLoopTemplate(executionId)} caught an exception and will be retried in {delay}", exception);
        }

        private void HandleStop(Guid executionId)
        {
            var stoppedAt = Volatile.Read(ref _stoppedAt);
            _logger.Debug($"{GetExecutionLoopTemplate(executionId)} stopped in {stoppedAt?.Elapsed.TotalMilliseconds ?? 0} ms");
        }

#if !NETSTANDARD1_3
        private void HandleThreadAbort(Guid executionId, Exception exception)
        {
            _logger.WarnException($"{GetExecutionLoopTemplate(executionId)} caught ThreadAbortException, see inner exception for details", exception);
        }
#endif

        private void ToRunningState()
        {
            lock (_stopped)
            {
                if (_disposed) return;

                if (_failedSince != null)
                {
                    // Since we are moving from the Failed state, one or more error messages were
                    // logged, and we should notify administrators that operations are restored.
                    _logger.Info($"{GetExecutionTemplate()} recovered from the Failed state after {_failedSince?.Elapsed} and is in the Running state now");
                }
                else if (_faultedSince != null)
                {
                    // We are moving from Faulted state, and there may be no any log messages (unless
                    // DEBUG level is enabled). But since some operations were delayed, we should notify
                    // administrators about this event, if a configured threshold is reached (to not to
                    // log thousands of messages).
                    _logger.Log(
                        _faultedSince.Elapsed > _options.WarningThreshold ? LogLevel.Info : LogLevel.Debug,
                        () => $"{GetExecutionTemplate()} recovered from the Faulted state after {_faultedSince?.Elapsed} and is in the Running state now");
                }

                _exceptionsCount = 0;

                _faultedSince = null;
                _failedSince = null;
                _lastException = null;
            }
        }

        private void ToFailedState(Exception exception, out TimeSpan retryDelay)
        {
            lock (_stopped)
            {
                retryDelay = FallbackRetryDelay;

                if (_disposed) return;

                _exceptionsCount++;
                _lastException = Stopwatch.StartNew();

                var optionsRetryDelay = _options.RetryDelay;
                if (optionsRetryDelay != null)
                {
                    retryDelay = optionsRetryDelay(_exceptionsCount);
                }

                NormalizeDelay(ref retryDelay);

                if (_faultedSince == null)
                {
                    _faultedSince = Stopwatch.StartNew();

                    // If threshold is zero, we'll go to the Failed state directly and log error anyway.
                    if (_options.ErrorThreshold > TimeSpan.Zero)
                    {
                        _logger.DebugException($"{GetExecutionTemplate()} is in the Faulted state now due to an exception, execution will be retried no more than in {retryDelay}", exception);
                    }
                }

                if (_failedSince == null && _faultedSince.Elapsed > _options.ErrorThreshold)
                {
                    // Transition to Failed state, we should log the error message.
                    _logger.ErrorException($"{GetExecutionTemplate()} is in the Failed state now due to an exception, execution will be retried no more than in {retryDelay}", exception);
                    _failedSince = Stopwatch.StartNew();
                }
                else if (_failedSince != null && _lastException.Elapsed >= _options.StillErrorThreshold)
                {
                    // Still in the Failed state, we should log the error message as a reminder,
                    // but shouldn't do this too often, especially for short retry intervals.
                    _logger.ErrorException($"{GetExecutionTemplate()} is still in the Failed state for {_failedSince?.Elapsed} due to an exception, will be retried no more than in {retryDelay}", exception);
                }
            }
        }

        private string GetExecutionLoopTemplate(Guid executionId)
        {
            return $"Execution loop {ToString()}:{executionId.ToString().Substring(0, 8)}";
        }

        private string GetExecutionTemplate()
        {
            return $"Execution {ToString()}";
        }

        private void SetStoppedAt()
        {
            Interlocked.CompareExchange(ref _stoppedAt, Stopwatch.StartNew(), null);
        }
    }
}
