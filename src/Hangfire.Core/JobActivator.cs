// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;

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
                    throw new ArgumentNullException("value");
                }

                _current = value;
            }
        }

        public virtual object ActivateJob(Type jobType)
        {
            return Activator.CreateInstance(jobType);
        }

        public virtual object ActivateJob(string jobType)
        {
            var type = Type.GetType(jobType);
            return type != null ? this.ActivateJob(type) : null;
        }        

        public virtual JobActivatorScope BeginScope()
        {
            return new SimpleJobActivatorScope(this);
        }

        class SimpleJobActivatorScope : JobActivatorScope
        {
            private readonly JobActivator _activator;
            private readonly List<IDisposable> _disposables = new List<IDisposable>();

            public SimpleJobActivatorScope([NotNull] JobActivator activator)
            {
                if (activator == null) throw new ArgumentNullException("activator");
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

            public override object Resolve(string typeName)
            {
                var instance = _activator.ActivateJob(typeName);
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
