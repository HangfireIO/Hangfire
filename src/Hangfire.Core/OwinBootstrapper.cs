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
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Server;
using Owin;

namespace Hangfire
{
    public static class OwinBootstrapper
    {
        /// <summary>
        /// Bootstraps Hangfire components using the given configuration
        /// action and maps Hangfire Dashboard to the app builder pipeline
        /// at the configured path ('/hangfire' by default).
        /// </summary>
        /// <param name="app">The app builder</param>
        /// <param name="configurationAction">Configuration action</param>
        public static void UseHangfire(
            [NotNull] this IAppBuilder app,
            [NotNull] Action<IBootstrapperConfiguration> configurationAction)
        {
            if (app == null) throw new ArgumentNullException("app");
            if (configurationAction == null) throw new ArgumentNullException("configurationAction");

            var configuration = new BootstrapperConfiguration();
            configurationAction(configuration);

            if (configuration.Activator != null)
            {
                JobActivator.Current = configuration.Activator;
            }

            if (configuration.Storage == null)
            {
                throw new InvalidOperationException("Job storage was not configured. Please call either `UseStorage` method or its overloads.");
            }

            JobStorage.Current = configuration.Storage;

            foreach (var filter in configuration.Filters)
            {
                GlobalJobFilters.Filters.Add(filter);
            }

            foreach (var server in configuration.Servers)
            {
                app.RunHangfireServer(server());
            }

            app.MapHangfireDashboard(configuration.DashboardPath, configuration.BackToSitePath, configuration.AuthorizationFilters);
        }
    }
}
