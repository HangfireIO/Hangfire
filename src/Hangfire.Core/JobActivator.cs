// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
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
using Hangfire.Annotations;
using Hangfire.Server;

namespace Hangfire
{
    public class JobActivator
    {
        private static JobActivator _current = new JobActivator();

        /// <summary>
        /// Gets or sets the current <see cref="JobActivator"/> instance 
        /// that will be used to activate jobs during performance.
        /// </summary>
        public static JobActivator Current
        {
            get { return _current; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _current = value;
            }
        }

        
        public virtual object ActivateJob(Type jobType)
        {
            return Activator.CreateInstance(jobType);
        }

        [Obsolete("Please implement/use the BeginScope(JobActivatorContext) method instead. Will be removed in 2.0.0.")]
        public virtual JobActivatorScope BeginScope()
        {
            return new SimpleJobActivatorScope(this);
        }

        public virtual JobActivatorScope BeginScope(JobActivatorContext context)
        {
#pragma warning disable 618
            return BeginScope();
#pragma warning restore 618
        }

        public virtual JobActivatorScope BeginScope(PerformContext context)
        {
            return this.BeginScope(new JobActivatorContext(context.Connection, context.BackgroundJob, context.CancellationToken));
        }

        class SimpleJobActivatorScope : JobActivatorScope
        {
            private readonly JobActivator _activator;
            private readonly List<IDisposable> _disposables = new List<IDisposable>();

            public SimpleJobActivatorScope([NotNull] JobActivator activator)
            {
                if (activator == null) throw new ArgumentNullException(nameof(activator));
                _activator = activator;
            }

            public override object Resolve(Type type)
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
