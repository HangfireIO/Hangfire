﻿// This file is part of Hangfire. Copyright © 2016 Hangfire OÜ.
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
using Hangfire.Dashboard;
using Hangfire.Server;
using Microsoft.AspNetCore.Builder;
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
using Microsoft.Extensions.Hosting;
#else
using Microsoft.AspNetCore.Hosting;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Hangfire.Common;

namespace Hangfire
{
    public static class HangfireApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHangfireDashboard(
            [NotNull] this IApplicationBuilder app,
            [NotNull] string pathMatch = "/hangfire",
            [CanBeNull] DashboardOptions options = null,
            [CanBeNull] JobStorage storage = null)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (pathMatch == null) throw new ArgumentNullException(nameof(pathMatch));

            HangfireServiceCollectionExtensions.ThrowIfNotConfigured(app.ApplicationServices);

            var services = app.ApplicationServices;

            storage = storage ?? services.GetRequiredService<JobStorage>();
            options = options ?? services.GetService<DashboardOptions>() ?? new DashboardOptions();
            options.TimeZoneResolver = options.TimeZoneResolver ?? services.GetService<ITimeZoneResolver>();

            var routes = app.ApplicationServices.GetRequiredService<RouteCollection>();

            app.Map(new PathString(pathMatch), x => x.UseMiddleware<AspNetCoreDashboardMiddleware>(storage, options, routes));

            return app;
        }

#if !NET451 && !NETSTANDARD1_3
        [Obsolete("Please use IServiceCollection.AddHangfireServer extension method instead in the ConfigureServices method. Will be removed in 2.0.0.")]
#endif
        public static IApplicationBuilder UseHangfireServer(
            [NotNull] this IApplicationBuilder app,
            [CanBeNull] BackgroundJobServerOptions options = null,
            [CanBeNull] IEnumerable<IBackgroundProcess> additionalProcesses = null,
            [CanBeNull] JobStorage storage = null)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            
            HangfireServiceCollectionExtensions.ThrowIfNotConfigured(app.ApplicationServices);

            var services = app.ApplicationServices;

            storage = storage ?? services.GetRequiredService<JobStorage>();
            options = options ?? services.GetService<BackgroundJobServerOptions>() ?? new BackgroundJobServerOptions();
            additionalProcesses = additionalProcesses ?? services.GetServices<IBackgroundProcess>();

            options.Activator = options.Activator ?? services.GetService<JobActivator>();
            options.FilterProvider = options.FilterProvider ?? services.GetService<IJobFilterProvider>();
            options.TimeZoneResolver = options.TimeZoneResolver ?? services.GetService<ITimeZoneResolver>();

            services.RegisterHangfireServer(HangfireServiceCollectionExtensions.GetInternalServices(services, out var factory, out var stateChanger, out var performer)
#pragma warning disable 618
                ? new BackgroundJobServer(options, storage, additionalProcesses, null, null, factory, performer, stateChanger)
#pragma warning restore 618
                : new BackgroundJobServer(options, storage, additionalProcesses));

            return app;
        }

        public static IApplicationBuilder UseHangfireServer(
            [NotNull] this IApplicationBuilder app,
            [NotNull] Func<IBackgroundProcessingServer> serverFactory)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (serverFactory == null) throw new ArgumentNullException(nameof(serverFactory));

            HangfireServiceCollectionExtensions.ThrowIfNotConfigured(app.ApplicationServices);
            app.ApplicationServices.RegisterHangfireServer(serverFactory());

            return app;
        }

        public static IServiceProvider RegisterHangfireServer(
            [NotNull] this IServiceProvider services,
            [NotNull] IBackgroundProcessingServer server)
        {
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
            var lifetime = services.GetRequiredService<IHostApplicationLifetime>();
#else
            var lifetime = services.GetRequiredService<IApplicationLifetime>();
#endif

            lifetime.ApplicationStopping.Register(server.SendStop);
            lifetime.ApplicationStopped.Register(server.Dispose);

            return services;
        }
    }
}
