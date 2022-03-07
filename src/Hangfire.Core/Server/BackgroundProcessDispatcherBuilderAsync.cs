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
using System.Linq;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Processing;

namespace Hangfire.Server
{
    internal sealed class BackgroundProcessDispatcherBuilderAsync : IBackgroundProcessDispatcherBuilder
    {
        private readonly int _maxConcurrency;
        private readonly bool _ownsScheduler;
        private readonly Func<TaskScheduler> _taskScheduler;
        private readonly IBackgroundProcessAsync _process;

        public BackgroundProcessDispatcherBuilderAsync(
            [NotNull] IBackgroundProcessAsync process,
            [NotNull] Func<TaskScheduler> taskScheduler,
            int maxConcurrency,
            bool ownsScheduler)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            if (taskScheduler == null) throw new ArgumentNullException(nameof(taskScheduler));
            if (maxConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

            _process = process;
            _taskScheduler = taskScheduler;
            _maxConcurrency = maxConcurrency;
            _ownsScheduler = ownsScheduler;
        }

        public IBackgroundDispatcher Create(BackgroundServerContext context, BackgroundProcessingServerOptions options)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var execution = new BackgroundExecution(
                context.StoppingToken,
                new BackgroundExecutionOptions
                {
                    Name = _process.GetType().Name,
                    RetryDelay = options.RetryDelay
                });

            return new BackgroundDispatcherAsync(
                execution,
                ExecuteProcess,
                Tuple.Create(_process, context, execution),
                _taskScheduler(),
                _maxConcurrency,
                _ownsScheduler);
        }

        public override string ToString()
        {
            return _process.GetType().Name;
        }

        private static async Task ExecuteProcess(Guid executionId, object state)
        {
            var tuple = (Tuple<IBackgroundProcessAsync, BackgroundServerContext, BackgroundExecution>)state;
            var serverContext = tuple.Item2;

            var context = new BackgroundProcessContext(
                serverContext.ServerId, 
                serverContext.Storage,
                serverContext.Properties.ToDictionary(x => x.Key, x => x.Value), 
                executionId, 
                serverContext.StoppingToken, 
                serverContext.StoppedToken,
                serverContext.ShutdownToken);

            while (!context.IsStopping)
            {
                await tuple.Item1.ExecuteAsync(context).ConfigureAwait(true);
                tuple.Item3.NotifySucceeded();
            }
        }
    }
}
