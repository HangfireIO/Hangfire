// This file is part of Hangfire. Copyright © 2021 Hangfire OÜ.
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
using Hangfire.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire
{
    internal sealed class DefaultClientManagerFactory : IBackgroundJobClientFactory, IRecurringJobManagerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultClientManagerFactory([NotNull] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IBackgroundJobClient GetClient(JobStorage storage)
        {
            if (HangfireServiceCollectionExtensions.GetInternalServices(_serviceProvider, out var factory, out var stateChanger, out _))
            {
                return new BackgroundJobClient(storage, factory, stateChanger);
            }

            return new BackgroundJobClient(
                storage,
                _serviceProvider.GetRequiredService<IJobFilterProvider>());
        }

        public IRecurringJobManager GetManager(JobStorage storage)
        {
            if (HangfireServiceCollectionExtensions.GetInternalServices(_serviceProvider, out var factory, out _, out _))
            {
                return new RecurringJobManager(
                    storage,
                    factory,
                    _serviceProvider.GetRequiredService<ITimeZoneResolver>());
            }

            return new RecurringJobManager(
                storage,
                _serviceProvider.GetRequiredService<IJobFilterProvider>(),
                _serviceProvider.GetRequiredService<ITimeZoneResolver>());
        }
    }
}