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
using Hangfire.Annotations;
using Hangfire.Processing;

#pragma warning disable 618

namespace Hangfire.Server
{
    internal sealed class ServerProcessDispatcherBuilder : IBackgroundProcessDispatcherBuilder
    {
        private readonly IServerComponent _component;
        private readonly Func<ThreadStart, IEnumerable<Thread>> _threadFactory;

        public ServerProcessDispatcherBuilder(
            [NotNull] IServerComponent component,
            [NotNull] Func<ThreadStart, IEnumerable<Thread>> threadFactory)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (threadFactory == null) throw new ArgumentNullException(nameof(threadFactory));
            _component = component;
            _threadFactory = threadFactory;
        }

        public IBackgroundDispatcher Create(BackgroundServerContext context, BackgroundProcessingServerOptions options)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (options == null) throw new ArgumentNullException(nameof(options));

            return new BackgroundDispatcher(
                new BackgroundExecution(context.StoppingToken, new BackgroundExecutionOptions
                {
                    Name = _component.GetType().Name,
                    RetryDelay = options.RetryDelay
                }),
                ExecuteComponent,
                Tuple.Create(_component, context),
                _threadFactory);
        }

        public override string ToString()
        {
            return _component.GetType().Name;
        }

        private static void ExecuteComponent(Guid executionId, object state)
        {
            var tuple = (Tuple<IServerComponent, BackgroundServerContext>)state;
            tuple.Item1.Execute(tuple.Item2.StoppingToken);
        }
    }
}
