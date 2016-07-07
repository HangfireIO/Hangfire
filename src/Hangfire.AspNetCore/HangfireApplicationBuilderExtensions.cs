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
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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

            ThrowIfNotConfigured(app);

            var services = app.ApplicationServices;

            storage = storage ?? services.GetRequiredService<JobStorage>();
            options = options ?? services.GetService<DashboardOptions>() ?? new DashboardOptions();
            var routes = app.ApplicationServices.GetRequiredService<RouteCollection>();

            app.Map(new PathString(pathMatch), x => x.UseMiddleware<AspNetCoreDashboardMiddleware>(storage, options, routes));

            return app;
        }

        public static IApplicationBuilder UseHangfireServer(
            [NotNull] this IApplicationBuilder app,
            [CanBeNull] BackgroundJobServerOptions options = null,
            [CanBeNull] IEnumerable<IBackgroundProcess> additionalProcesses = null,
            [CanBeNull] JobStorage storage = null)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            
            ThrowIfNotConfigured(app);

            var services = app.ApplicationServices;
            var lifetime = services.GetRequiredService<IApplicationLifetime>();

            storage = storage ?? services.GetRequiredService<JobStorage>();
            options = options ?? services.GetService<BackgroundJobServerOptions>() ?? new BackgroundJobServerOptions();
            additionalProcesses = additionalProcesses ?? services.GetServices<IBackgroundProcess>();

            var server = new BackgroundJobServer(options, storage, additionalProcesses);

            lifetime.ApplicationStopping.Register(() => server.SendStop());
            lifetime.ApplicationStopped.Register(() => server.Dispose());

            return app;
        }

        private static void ThrowIfNotConfigured(IApplicationBuilder app)
        {
            var configuration = app.ApplicationServices.GetService<IGlobalConfiguration>();
            if (configuration == null)
            {
                throw new InvalidOperationException(
                    "Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddHangfire' inside the call to 'ConfigureServices(...)' in the application startup code.");
            }
        }
    }
}
