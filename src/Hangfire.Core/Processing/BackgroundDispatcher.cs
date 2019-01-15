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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Logging;
using ThreadState = System.Threading.ThreadState;

namespace Hangfire.Processing
{
    internal sealed class BackgroundDispatcher : IBackgroundDispatcher
    {
        private readonly ILog _logger = LogProvider.GetLogger(typeof(BackgroundDispatcher));
        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);
        private readonly TaskCompletionSource<object> _stoppedTcs = new TaskCompletionSource<object>();

        private readonly IBackgroundExecution _execution;
        private readonly Action<Guid, object> _action;
        private readonly object _state;

        private int _running;

        public BackgroundDispatcher(
            [NotNull] IBackgroundExecution execution,
            [NotNull] Action<Guid, object> action,
            [CanBeNull] object state,
            [NotNull] Func<ThreadStart, IEnumerable<Thread>> threadFactory)
        {
            if (threadFactory == null) throw new ArgumentNullException(nameof(threadFactory));
            if (execution == null) throw new ArgumentNullException(nameof(execution));
            if (action == null) throw new ArgumentNullException(nameof(action));

            _execution = execution;
            _action = action;
            _state = state;

#if !NETSTANDARD1_3
            AppDomainUnloadMonitor.EnsureInitialized();
#endif

            var threads = threadFactory(DispatchLoop)?.ToArray();

            if (threads == null || threads.Length == 0)
            {
                throw new ArgumentException("At least one unstarted thread should be created.", nameof(threadFactory));
            }

            if (threads.Any(thread => thread == null || (thread.ThreadState & ThreadState.Unstarted) == 0))
            {
                throw new ArgumentException("All the threads should be non-null and in the ThreadState.Unstarted state.", nameof(threadFactory));
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }
        }

        public bool Wait(TimeSpan timeout)
        {
            return _stopped.WaitOne(timeout);
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await await Task.WhenAny(_stoppedTcs.Task, cancellationToken.AsTask());
        }

        public void Dispose()
        {
            _execution.Dispose();
            _stopped.Dispose();
        }

        public override string ToString()
        {
            return _execution.ToString();
        }

        private void DispatchLoop()
        {
            var incrementedRunning = false;

            try
            {
                try { }
                finally
                {
                    Interlocked.Increment(ref _running);
                    incrementedRunning = true;
                }

                _execution.Run(_action, _state);
            }
            catch (Exception ex)
            {
#if !NETSTANDARD1_3
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
#if !NETSTANDARD1_3
                        // todo add original and current exceptions
                        Trace.WriteLine("Unexpected exception occurred while logging an exception: ");
#endif
                    }
                }
            }
            finally
            {
                try
                {
                    if (incrementedRunning && Interlocked.Decrement(ref _running) == 0)
                    {
                        _stopped.Set();
                        _stoppedTcs.SetResult(null);
                    }
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
    }
}