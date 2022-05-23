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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Processing;

namespace Hangfire.Server
{
    internal sealed class BackgroundProcessDispatcherBuilder : IBackgroundProcessDispatcherBuilder
    {
        private readonly IBackgroundProcess _process;
        private readonly Func<ThreadStart, IEnumerable<Thread>> _threadFactory;

        public BackgroundProcessDispatcherBuilder(
            [NotNull] IBackgroundProcess process,
            [NotNull] Func<ThreadStart, IEnumerable<Thread>> threadFactory)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            if (threadFactory == null) throw new ArgumentNullException(nameof(threadFactory));

            _process = process;
            _threadFactory = threadFactory;
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

            return new BackgroundDispatcher(
                execution,
                ExecuteProcess,
                Tuple.Create(_process, context, execution),
                _threadFactory);
        }

        public override string ToString()
        {
            return _process.GetType().Name;
        }

        private static void ExecuteProcess(Guid executionId, object state)
        {
            var tuple = (Tuple<IBackgroundProcess, BackgroundServerContext, BackgroundExecution>)state;
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
                tuple.Item1.Execute(context);
                tuple.Item3.NotifySucceeded();
            }
        }
    }
}
