// This file is part of Hangfire. Copyright © 2016 Hangfire OÜ.
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
using Hangfire.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.AspNetCore
{
    internal class AspNetCoreJobActivatorScope : JobActivatorScope
    {
        private readonly IServiceScope _serviceScope;

        public AspNetCoreJobActivatorScope([NotNull] IServiceScope serviceScope)
        {
            if (serviceScope == null) throw new ArgumentNullException(nameof(serviceScope));
            _serviceScope = serviceScope;
        }

        public override object Resolve(Type type)
        {
            return ActivatorUtilities.GetServiceOrCreateInstance(_serviceScope.ServiceProvider, type);
        }

        public override void DisposeScope()
        {
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1
            if (_serviceScope is IAsyncDisposable asyncDisposable)
            {
                // Service scope disposal is triggered inside a dedicated background thread,
                // while Task result is being set in CLR's Thread Pool, so no deadlocks on
                // wait should happen.
                asyncDisposable.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                return;
            }
#endif
            _serviceScope.Dispose();
        }
    }
}