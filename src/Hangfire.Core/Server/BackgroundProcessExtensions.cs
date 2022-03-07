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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.Processing;

namespace Hangfire.Server
{
    public static class BackgroundProcessExtensions
    {
        public static IBackgroundProcessDispatcherBuilder UseBackgroundPool(
            [NotNull] this IBackgroundProcess process)
        {
            return UseBackgroundPool(process, Environment.ProcessorCount);
        }

        public static IBackgroundProcessDispatcherBuilder UseBackgroundPool(
            [NotNull] this IBackgroundProcess process,
            int threadCount)
        {
            return UseBackgroundPool(process, threadCount, null);
        }

        public static IBackgroundProcessDispatcherBuilder UseBackgroundPool(
            [NotNull] this IBackgroundProcess process,
            int threadCount,
            [CanBeNull] Action<Thread> threadConfig)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            if (threadCount <= 0) throw new ArgumentOutOfRangeException(nameof(threadCount));

            return UseBackgroundPool(
                process,
                (threadName, threadStart) => DefaultThreadFactory(threadCount, threadName, threadStart, threadConfig));
        }

        public static IBackgroundProcessDispatcherBuilder UseBackgroundPool(
            [NotNull] this IBackgroundProcess process,
            [NotNull] Func<string, ThreadStart, IEnumerable<Thread>> threadFactory)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            if (threadFactory == null) throw new ArgumentNullException(nameof(threadFactory));

            return new BackgroundProcessDispatcherBuilder(
                process,
                threadStart => threadFactory(process.GetType().Name, threadStart));
        }

        public static IBackgroundProcessDispatcherBuilder UseBackgroundPool(
            [NotNull] this IBackgroundProcessAsync process)
        {
            return UseBackgroundPool(process, Environment.ProcessorCount);
        }

        public static IBackgroundProcessDispatcherBuilder UseBackgroundPool(
            [NotNull] this IBackgroundProcessAsync process,
            int maxConcurrency)
        {
            return UseBackgroundPool(
                process,
                maxConcurrency,
                maxConcurrency < Environment.ProcessorCount ? maxConcurrency : Environment.ProcessorCount);
        }

        public static IBackgroundProcessDispatcherBuilder UseBackgroundPool(
            [NotNull] this IBackgroundProcessAsync process,
            int maxConcurrency,
            int threadCount)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            if (maxConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
            if (threadCount <= 0) throw new ArgumentOutOfRangeException(nameof(threadCount));

            return UseBackgroundPool(
                process,
                maxConcurrency,
                (threadName, threadStart) => DefaultThreadFactory(threadCount, threadName, threadStart));
        }

        public static IBackgroundProcessDispatcherBuilder UseBackgroundPool(
            [NotNull] this IBackgroundProcessAsync process,
            int maxConcurrency,
            [NotNull] Func<string, ThreadStart, IEnumerable<Thread>> threadFactory)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            if (maxConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
            if (threadFactory == null) throw new ArgumentNullException(nameof(threadFactory));

            Func<TaskScheduler> createScheduler = () => new BackgroundTaskScheduler(
                threadStart => threadFactory(process.GetType().Name, threadStart),
                exception =>
                {
                    LogProvider.GetLogger(typeof(BackgroundTaskScheduler)).FatalException(
                        "Unhandled exception occurred in scheduler. Please report it to Hangfire developers",
                        exception);
                });

            return new BackgroundProcessDispatcherBuilderAsync(process, createScheduler, maxConcurrency, true);
        }

        public static IBackgroundProcessDispatcherBuilder UseThreadPool(
            [NotNull] this IBackgroundProcessAsync process)
        {
            return UseThreadPool(process, Environment.ProcessorCount);
        }

        public static IBackgroundProcessDispatcherBuilder UseThreadPool(
            [NotNull] this IBackgroundProcessAsync process,
            int maxConcurrency)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            if (maxConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

            return new BackgroundProcessDispatcherBuilderAsync(process, () => TaskScheduler.Default, maxConcurrency, false);
        }

        internal static IEnumerable<Thread> DefaultThreadFactory(
            int threadCount,
            [NotNull] string threadName,
            [NotNull] ThreadStart threadStart,
            [CanBeNull] Action<Thread> threadConfig = null)
        {
            if (threadName == null) throw new ArgumentNullException(nameof(threadName));
            if (threadStart == null) throw new ArgumentNullException(nameof(threadStart));
            if (threadCount <= 0) throw new ArgumentOutOfRangeException(nameof(threadCount));

            for (var i = 0; i < threadCount; i++)
            {
                var thread = new Thread(threadStart)
                {
                    IsBackground = true,
                    Name = $"{threadName} #{i + 1}"
                };

                threadConfig?.Invoke(thread);
                yield return thread;
            }
        }
    }
}
