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
