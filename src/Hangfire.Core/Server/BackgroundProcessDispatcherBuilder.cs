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
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Processing;

namespace Hangfire.Server
{
    public sealed class BackgroundProcessDispatcherBuilder : IBackgroundProcessDispatcherBuilder
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

        public IBackgroundDispatcher Create(BackgroundProcessContext context, BackgroundProcessingServerOptions options)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (options == null) throw new ArgumentNullException(nameof(options));

            return new BackgroundDispatcher(
                new BackgroundExecution(context.CancellationToken, context.AbortToken, new BackgroundExecutionOptions
                {
                    Name = _process.GetType().Name,
                    RetryDelay = options.RetryDelay
                }),
                ExecuteProcess,
                Tuple.Create(_process, context),
                _threadFactory);
        }

        public override string ToString()
        {
            return _process.GetType().Name;
        }

        private static void ExecuteProcess(object state)
        {
            var context = (Tuple<IBackgroundProcess, BackgroundProcessContext>)state;
            context.Item1.Execute(context.Item2);
        }
    }
}
