// This file is part of Hangfire.
// Copyright © 2016 Sergey Odinokov.
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
            return _serviceScope.ServiceProvider.GetRequiredService(type);
        }

        public override void DisposeScope()
        {
            _serviceScope.Dispose();
        }
    }
}