// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using Hangfire.Annotations;
using Hangfire.Server;

namespace Hangfire
{
    public class JobActivator
    {
        /// <summary>
        /// Gets or sets the current <see cref="JobActivator"/> instance 
        /// that will be used to activate jobs during performance.
        /// </summary>
        [NotNull]
        public static JobActivator Current
        {
            get => GlobalConfiguration.Configuration.ResolveService<JobActivator>();

            [Obsolete]
            set => GlobalConfiguration.Configuration.UseActivator(value);
        }

        [CanBeNull]
        public virtual object? ActivateJob([NotNull] Type jobType)
        {
            return Activator.CreateInstance(jobType);
        }

        [Obsolete("Please implement/use the BeginScope(JobActivatorContext) method instead. Will be removed in 2.0.0.")]
        public virtual JobActivatorScope BeginScope()
        {
            return new SimpleJobActivatorScope(this);
        }

        [NotNull]
        public virtual JobActivatorScope BeginScope([NotNull] JobActivatorContext context)
        {
#pragma warning disable 618
            return BeginScope();
#pragma warning restore 618
        }

        [NotNull]
        public virtual JobActivatorScope BeginScope([NotNull] PerformContext context)
        {
            return BeginScope(new JobActivatorContext(context.Connection, context.BackgroundJob, context.CancellationToken));
        }

        class SimpleJobActivatorScope : JobActivatorScope
        {
            private readonly JobActivator _activator;
            private readonly List<IDisposable> _disposables = new List<IDisposable>();

            public SimpleJobActivatorScope([NotNull] JobActivator activator)
            {
                _activator = activator ?? throw new ArgumentNullException(nameof(activator));
            }

            public override object? Resolve(Type type)
            {
                var instance = _activator.ActivateJob(type);
                var disposable = instance as IDisposable;

                if (disposable != null)
                {
                    _disposables.Add(disposable);
                }

                return instance;
            }

            public override void DisposeScope()
            {
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
