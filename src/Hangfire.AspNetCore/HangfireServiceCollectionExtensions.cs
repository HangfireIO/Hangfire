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
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Dashboard;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hangfire
{
    public static class HangfireServiceCollectionExtensions
    {
        public static IServiceCollection AddHangfire(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<IGlobalConfiguration> configuration)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            services.TryAddSingleton(_ => JobStorage.Current);
            services.TryAddSingleton(_ => JobActivator.Current);
            services.TryAddSingleton(_ => DashboardRoutes.Routes);
            services.TryAddSingleton<IJobFilterProvider>(_ => GlobalJobFilters.Filters);

            services.TryAddSingleton<IBackgroundJobFactory, BackgroundJobFactory>();
            services.TryAddSingleton<IBackgroundJobStateChanger, BackgroundJobStateChanger>();
            services.TryAddSingleton<IBackgroundJobClient, BackgroundJobClient>();

            services.TryAddSingleton(typeof(HangfireMarkerService));
            services.TryAddSingleton(_ => configuration);

            return services;
        }
    }
}
