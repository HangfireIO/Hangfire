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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using ThreadState = System.Threading.ThreadState;

namespace Hangfire.Processing
{
    /// <summary>Represents a custom implementation of the <see cref="TaskScheduler"/> that uses
    /// its own threads to execute <see cref="Task"/>-based work items and their continuations.
    /// The primary purpose of this scheduler is background processing, for other use cases
    /// consider using the <see cref="TaskScheduler.Default"/> scheduler instead.</summary>
    /// <remarks>
    /// <para>You can use this scheduler to offload background tasks to a separate, dedicated
    /// pool of threads, instead of executing them in ThreadPool's threads.
    /// Background work items don't usually affect the request/response logic directly, so we
    /// can afford some additional latency and execute them, when no foreground processing is
    /// held. This is useful, when you want to minimize your response latencies to their minimum,
    /// and don't want to allow background processing to affect the foreground one.</para>
    /// 
    /// <para>It is not possible to offload *all* the work to dedicated threads, because a lot of
    /// libraries hard-code their usage of the default thread pool in one way or another: by using 
    /// the ConfigureAwait(false) method, by explicitly creating continuations on TaskScheduler.Default,
    /// or simply by using ThreadPool.QueueUserWorkItem method. So this is a best-effort attempt.</para>
    /// 
    /// <para>Please note that all unprocessed work items are lost, when the <see cref="Dispose"/>
    /// method is called or the corresponding AppDomain is unloaded (for example,
    /// due to process shutdown). In order to survive the process restarts, use different solutions
    /// with persistence, like <see href="https://www.hangfire.io">Hangfire</see>.</para>
    /// </remarks>
    /// 
    /// <threadsafety static="true" instance="true"/>
    public sealed class BackgroundTaskScheduler : TaskScheduler, IDisposable
    {
        // Single global queue is used instead of work stealing one for simplified maintenance,
        // and because it's enough, when dealing with async/await methods, since they rarely
        // enqueue continuations to local queues (comparing to ContinueWith-based continuations).
        private readonly ConcurrentQueue<Task> _queue = new ConcurrentQueue<Task>();

        private readonly Thread[] _threads;
        private readonly HashSet<int> _ourThreadIds;

        // Regular semaphore is used to perform waits, when there are no tasks to be
        // processed. It doesn't use any kind of busy waiting to allow "foreground"
        // schedulers to perform useful work, instead of consuming CPU time by spinning
        // with the hope of a task arrival. It is used with care with no unnecessary
        // operations involved.
        private readonly Semaphore _semaphore;
        private readonly ManualResetEvent _stopped = new ManualResetEvent(false);

        private readonly WaitHandle[] _waitHandles;
        private readonly Action<Exception> _exceptionHandler;

        private int _disposed;

        /// <summary>Initializes a new instance of the <see cref="BackgroundTaskScheduler"/> with
        /// the number of threads based on the <see cref="Environment.ProcessorCount"/> property.
        /// All the created threads will be started to dispatch <see cref="Task"/>
        /// instances scheduled to run on this scheduler.</summary>
        public BackgroundTaskScheduler()
            : this(Environment.ProcessorCount)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="BackgroundTaskScheduler"/> with
        /// the given number of dedicated threads that will be creating using the default thread
        /// factory. All the created threads will be started to dispatch <see cref="Task"/>
        /// instances scheduled to run on this scheduler.</summary>
        /// <param name="threadCount">The number of dedicated threads will be created.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="threadCount"/> is less or equal to zero.</exception>
        public BackgroundTaskScheduler(int threadCount)
            : this(threadStart => DefaultThreadFactory(threadStart, threadCount), DefaultExceptionHandler)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="BackgroundTaskScheduler"/>
        /// class with the specified <paramref name="threadFactory"/> and an optional exception
        /// handler. All the created threads will be started to dispatch <see cref="Task"/>
        /// instances scheduled to run on this scheduler.</summary>
        /// <param name="threadFactory">Callback that creates one or more dedicated threads.</param>
        /// <param name="exceptionHandler">Optional callback that is invoked when unhandled exception occurs 
        /// in one of the threads. After this event this instance is considered stopped.</param>
        /// <exception cref="ArgumentNullException"><paramref name="threadFactory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="threadFactory"/> returned <see langword="null"/> or zero threads.</exception>
        /// <exception cref="ArgumentException"><paramref name="threadFactory"/> returned at least one thread not in the <see cref="ThreadState.Unstarted"/> state.</exception>
        public BackgroundTaskScheduler(
            [NotNull] Func<ThreadStart, IEnumerable<Thread>> threadFactory,
            [CanBeNull] Action<Exception> exceptionHandler)
        {
            if (threadFactory == null) throw new ArgumentNullException(nameof(threadFactory));

            _exceptionHandler = exceptionHandler;
            _semaphore = new Semaphore(0, Int32.MaxValue);

            // Stopped event should always be the first in this array, see the DispatchLoop method.
            _waitHandles = new WaitHandle[] { _stopped, _semaphore };

#if !NETSTANDARD1_3
            AppDomainUnloadMonitor.EnsureInitialized();
#endif

            _threads = threadFactory(DispatchLoop)?.ToArray();

            if (_threads == null || _threads.Length == 0)
            {
                throw new ArgumentException("At least one non-started thread should be created.", nameof(threadFactory));
            }

            if (_threads.Any(thread => thread == null || (thread.ThreadState & ThreadState.Unstarted) == 0))
            {
                throw new ArgumentException("All the threads should be non-null and in the ThreadState.Unstarted state.", nameof(threadFactory));
            }

            foreach (var thread in _threads)
            {
                thread.Start();
            }

            _ourThreadIds = new HashSet<int>(_threads.Select(x => x.ManagedThreadId));
        }

        /// <inheritdoc />
        public override int MaximumConcurrencyLevel => _threads.Length;

        /// <summary>Signals all the threads to be stopped and releases all the unmanaged resources.
        /// This method should be called only when you are uninterested on the corresponding tasks,
        /// i.e. during AppDomain unloads, process shutdowns, etc.</summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
            {
                return;
            }

            _stopped.Set();

            // We don't wait for threads here using Thread.Join method, because we can't
            // guarantee that all the threads will be stopped after a call to the Dispose 
            // method without introducing an infinite block (they are executing user code,
            // and there can be anything).
            // Since the Dispose method is usually called from within a protected region
            // itself (such as when using the `using` statements), we shouldn't block here,
            // because this may prevent AppDomain from being unloaded, causing unexpected
            // behavior for applications.

            // Since we don't wait until completion, our threads are responsible for preventing
            // or catching ObjectDisposedException from these wait handles.

            _stopped.Dispose();
            _semaphore.Dispose();
        }

        /// <inheritdoc />
        protected override void QueueTask(Task task)
        {
            // Both ConcurrentQueue and Semaphore may cause a thread to be blocked. During that
            // transition, CLR is checking whether there are any Thread Interrupts pending and
            // throws if any. Any exception thrown from this method will lead to an unobserved
            // exception posted to ThreadPool due to the internal implementation of TaskScheduler,
            // and this may bring the whole process down.
            // To prevent this, WaitHandle.WaitAny is required in the work loop.

            _queue.Enqueue(task);
            _semaphore.Release();
        }

        /// <inheritdoc />
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // There's no efficient way to dequeue an arbitrary element from concurrent queue.
            if (taskWasPreviouslyQueued) return false;

            // This method can be called from other schedulers that want to get some advice
            // whether or not inline execution of tasks that belongs to the current scheduler.
            // Since we want to execute as much tasks as possible on our dedicated threads,
            // we allow to inline only requests from the current scheduler, i.e. just to save
            // some time, since no queueing will be involved.

            if (!_ourThreadIds.Contains(Thread.CurrentThread.ManagedThreadId)) return false;

            return TryExecuteTask(task);
        }

        /// <inheritdoc />
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            ThrowIfDisposed();
            return _queue.ToArray();
        }

        private static IEnumerable<Thread> DefaultThreadFactory(ThreadStart threadStart, int threadCount)
        {
            if (threadCount <= 0) throw new ArgumentOutOfRangeException(nameof(threadCount));
            var threads = new Thread[threadCount];

            for (var i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(threadStart)
                {
                    Name = $"BackgroundThread #{i + 1}",
                    IsBackground = true,
                };
            }

            return threads;
        }

        private static void DefaultExceptionHandler(Exception exception)
        {
#if !NETSTANDARD1_3
            Trace.WriteLine("An unhandled exception occurred: " + exception);
#endif
        }

        private void DispatchLoop()
        {
            try
            {
                // The outer loop is needed to keep threads under our control by catching
                // TIE and TAE exceptions to prevent their destructive behavior of killing
                // our threads without need to re-create them as implemented in ThreadPool.
                // Unfortunately we don't have the same reset mechanisms available, because
                // they are native and aren't exposed to public, so we'll do this on a best
                // effort basis.

                // TODO: Reset Thread.Name, Thread.CurrentCulture and Thread.Priority?
                // What about ExecutionContext and SynchronizationContext? But we don't have
                // public APIs to handle this.

                while (Volatile.Read(ref _disposed) == 0)
                {
                    Task task = null;

                    try
                    {
                        // Kernel wait is required here, because otherwise any pending thread
                        // interrupt may kill the whole process, because of internal TaskScheduler
                        // implementation. By calling WaitHandle.WaitAny here, we are causing
                        // CLR to check if there are pending interrupts earlier, and in the
                        // place where we can safely handle it.
                        
                        while (WaitHandle.WaitAny(_waitHandles) != 0)
                        {
                            if (_queue.TryDequeue(out task))
                            {
                                TryExecuteTask(task);
                            }
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // There's a benign race, when wait handles are disposed just
                        // before the call to WaitAny. Other methods don't throw exceptions
                        // of this type. Since this is an ordinal shutdown, we can skip
                        // the reporting logic.
                    }
#if !NETSTANDARD1_3
                    catch (Exception ex) when (ex is ThreadAbortException || ex is ThreadInterruptedException)
                    {
                        // We don't have methods like IThreadPoolWorkItem.MarkAborted in public
                        // API, but we should handle non-completed task in case of TIE or TAE,
                        // because there's a high probability to lose continuations. So we're
                        // simply re-queueing it.
                        if (task != null && !task.IsCompleted)
                        {
                            QueueTask(task);
                        }
                        
                        // Normally, there should be no check for AppDomain unload condition, because we can't
                        // get here on appdomain unloads. But Mono < 5.4 has an issue with Thread.ResetAbort, and
                        // it can prevent appdomain to be unloaded: https://bugzilla.xamarin.com/show_bug.cgi?id=5804.
                        // It's better to reassure this can't happen under all circumstances.
                        if ((Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) != 0 && 
                            !AppDomainUnloadMonitor.IsUnloading)
                        {
                            try
                            {
                                Thread.ResetAbort();
                            }
                            catch (PlatformNotSupportedException)
                            {
                            }
                        }
                    }
#endif
                }
            }
#if !NETSTANDARD1_3
            catch (ThreadAbortException ex)
            {
                // ThreadAbortException is expected during AppDomain unloads, so we
                // don't call the exception handler in this case.
                if (!AppDomainUnloadMonitor.IsUnloading)
                {
                    InvokeUnhandledExceptionHandler(ex);
                }
            }
#endif
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                InvokeUnhandledExceptionHandler(ex);
            }
        }

        private void InvokeUnhandledExceptionHandler(Exception exception)
        {
            try
            {
                var handler = _exceptionHandler;
                handler?.Invoke(exception);
            }
#if !NETSTANDARD1_3
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                Trace.WriteLine("Unexpected exception caught in exception handler itself." + Environment.NewLine + ex);
            }
#else
            catch
            {
            }
#endif
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) == 1)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
