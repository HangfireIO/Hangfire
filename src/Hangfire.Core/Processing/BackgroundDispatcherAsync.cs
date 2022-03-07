// This file is part of Hangfire. Copyright © 2017 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;
#if !NETSTANDARD1_3
using System.Diagnostics;
#endif
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Logging;

namespace Hangfire.Processing
{
    internal sealed class BackgroundDispatcherAsync : IBackgroundDispatcher
    {
        private readonly ILog _logger = LogProvider.GetLogger(typeof(BackgroundDispatcherAsync));
        private readonly CountdownEvent _stopped;

        private readonly IBackgroundExecution _execution;
        private readonly Func<Guid, object, Task> _action;
        private readonly object _state;

        private readonly TaskScheduler _taskScheduler;
        private readonly bool _ownsScheduler;

        public BackgroundDispatcherAsync(
            [NotNull] IBackgroundExecution execution,
            [NotNull] Func<Guid, object, Task> action,
            [CanBeNull] object state,
            [NotNull] TaskScheduler taskScheduler,
            int maxConcurrency,
            bool ownsScheduler)
        {
            if (maxConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

            _execution = execution ?? throw new ArgumentNullException(nameof(execution));
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _state = state;
            _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
            _ownsScheduler = ownsScheduler;

#if !NETSTANDARD1_3
            AppDomainUnloadMonitor.EnsureInitialized();
#endif

            _stopped = new CountdownEvent(maxConcurrency);

            for (var i = 0; i < maxConcurrency; i++)
            {
                Task.Factory.StartNew(
                    DispatchLoop,
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    _taskScheduler).Unwrap();
            }
        }

        public bool Wait(TimeSpan timeout)
        {
            return _stopped.WaitHandle.WaitOne(timeout);
        }

        public async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            await _stopped.WaitHandle.WaitOneAsync(timeout, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _execution.Dispose();
            _stopped.Dispose();

            if (_ownsScheduler && _taskScheduler is IDisposable disposableScheduler)
            {
                disposableScheduler.Dispose();
            }
        }

        public override string ToString()
        {
            return _execution.ToString();
        }

        private async Task DispatchLoop()
        {
            try
            {
                await _execution.RunAsync(_action, _state).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
#if !NETSTANDARD1_3
                if (!(ex is ThreadAbortException) || !AppDomainUnloadMonitor.IsUnloading)
#endif
                {
                    try
                    {
                        _logger.FatalException("Dispatcher is stopped due to an exception, you need to restart the server manually. Please report it to Hangfire developers.", ex);
                    }
                    catch
                    {
#if !NETSTANDARD1_3
                        Debug.WriteLine($"Dispatcher is stopped due to an exception, you need to restart the server manually. Please report it to Hangfire developers: {ex}");
#endif
                    }
                }
            }
            finally
            {
                try
                {
                    _stopped.Signal();
                }
                catch (ObjectDisposedException)
                {
#if !NETSTANDARD1_3
                    Debug.WriteLine("Unable to signal the stopped event for BackgroundDispatcher: it was already disposed");
#endif
                }
            }
        }
    }
}