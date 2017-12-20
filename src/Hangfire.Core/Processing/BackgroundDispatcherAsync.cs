// This file is part of Hangfire.
// Copyright © 2017 Sergey Odinokov.
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

namespace Hangfire.Processing
{
    internal sealed class BackgroundDispatcherAsync : IBackgroundDispatcher
    {
        private readonly ILog _logger = LogProvider.GetLogger(typeof(BackgroundDispatcherAsync));
        private readonly Func<object, Task> _action;
        private readonly object _state;
        private readonly IBackgroundExecution _execution;
        private readonly TaskScheduler _taskScheduler;
        private readonly Task[] _tasks;

        public BackgroundDispatcherAsync(
            [NotNull] IBackgroundExecution execution,
            [NotNull] Func<object, Task> action,
            [CanBeNull] object state,
            [NotNull] TaskScheduler taskScheduler,
            int maxConcurrency,
            bool ownsScheduler)
        {
            if (maxConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

            _action = action ?? throw new ArgumentNullException(nameof(action));
            _state = state;
            _execution = execution ?? throw new ArgumentNullException(nameof(execution));
            _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
            _tasks = new Task[maxConcurrency];

#if NETFULL
            AppDomainUnloadMonitor.EnsureInitialized();
#endif

            for (var i = 0; i < maxConcurrency; i++)
            {
                _tasks[i] = Task.Factory.StartNew(
                    DispatchLoop,
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    _taskScheduler).Unwrap();
            }

            if (ownsScheduler)
            {
                // When we do own a disposable scheduler, i.e. it was created solely for the
                // usage by this dispatcher, we are responsible for its disposal to stop all
                // the threads and release their resources. We can only dispose it when there's
                // no outstanding work, i.e. when dispatcher was stopped.
                // todo: this will not fire when appdomain is unloaded

                Task.WhenAll(_tasks).ContinueWith(
                    DisposeScheduler,
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default); // todo explain why default
            }
        }

        public bool Wait(TimeSpan timeout)
        {
            try
            {
                // todo we can't wait while async method
                // todo better way to wait on task
                //todo remove timeout
                return Task.WaitAll(_tasks, timeout);
            }
            catch (AggregateException)
            {
                // Task.WaitAll throws AggregateException, when at least one of
                // the given tasks was canceled or thrown an exception. We can't
                // get TaskCancelledException, because our task can't be canceled,
                // because no CancellationToken is specified. And can't throw any
                // exception except ThreadAbortedException
                // Aggregate todo
                // We aren't interested in thread exceptions, all of them
                // are logged by the dispatcher itself.
            }

            return true;
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // todo why double await
                // OCE should be thrown outside
                await await Task.WhenAny(Task.WhenAll(_tasks), cancellationToken.AsTask()).ConfigureAwait(false);
            }
            catch (AggregateException)
            {
                // todo check this
                // We aren't interested in thread exceptions, all of them
                // are logged by the dispatcher itself.
            }
        }

        public void Dispose()
        {
            _execution.Dispose();
        }

        public override string ToString()
        {
            return _execution.ToString();
        }

        private void DisposeScheduler(Task antecedent)
        {
            var disposableScheduler = _taskScheduler as IDisposable;
            disposableScheduler?.Dispose();
        }

        private async Task DispatchLoop()
        {
            try
            {
                await _execution.RunAsync(_action, _state);
            }
            catch (Exception ex)
            {
#if NETFULL
                if (!(ex is ThreadAbortException) || !AppDomainUnloadMonitor.IsUnloading)
#endif
                {
                    try
                    {
                        // todo explain dispatcher is stopped
                        _logger.FatalException("Unexpected exception occurred in BackgroundDispatcher. Please report it to developers.", ex);
                    }
                    catch
                    {
#if NETFULL
                        // todo add original and current exceptions
                        Trace.WriteLine("Unexpected exception occurred while logging an exception: ");
#endif
                    }
                }
            }
        }
    }
}