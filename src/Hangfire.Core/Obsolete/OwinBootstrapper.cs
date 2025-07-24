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
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Server;
using Owin;

namespace Hangfire
{
    /// <exclude />
    [Obsolete("Please use `GlobalConfiguration` class for configuration, or `IAppBuilder.UseHangfireDashboard` and `IAppBuilder.UseHangfireServer` OWIN extension methods instead. Will be removed in version 2.0.0.")]
    public static class OwinBootstrapper
    {
        /// <summary>
        /// Bootstraps Hangfire components using the given configuration
        /// action and maps Hangfire Dashboard to the app builder pipeline
        /// at the configured path ('/hangfire' by default).
        /// </summary>
        /// <param name="app">The app builder</param>
        /// <param name="configurationAction">Configuration action</param>
        [Obsolete("Please use `GlobalConfiguration` class for configuration, or `IAppBuilder.UseHangfireDashboard` and `IAppBuilder.UseHangfireServer` OWIN extension methods instead. Will be removed in version 2.0.0.")]
        public static void UseHangfire(
            [NotNull] this IAppBuilder app,
            [NotNull] Action<IBootstrapperConfiguration> configurationAction)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (configurationAction == null) throw new ArgumentNullException(nameof(configurationAction));

            var configuration = new BootstrapperConfiguration();
            configurationAction(configuration);

            if (configuration.Activator != null)
            {
                JobActivator.Current = configuration.Activator;
            }

            if (configuration.Storage != null)
            {
                JobStorage.Current = configuration.Storage;
            }

            foreach (var filter in configuration.Filters)
            {
                GlobalJobFilters.Filters.Add(filter);
            }

            foreach (var server in configuration.Servers)
            {
                app.RunHangfireServer(server());
            }

            app.MapHangfireDashboard(configuration.DashboardPath, configuration.AppPath, configuration.AuthorizationFilters);
        }
    }
}
