// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using HangFire.Annotations;
using HangFire.Dashboard;
using HangFire.Server;
using Owin;

namespace HangFire
{
    public static class OwinBootstrapper
    {
        /// <summary>
        /// Bootstraps HangFire components using the given configuration
        /// action and maps HangFire Dashboard to the app builder pipeline
        /// at the configured path ('/hangfire' by default).
        /// </summary>
        /// <param name="app">The app builder</param>
        /// <param name="configurationAction">Configuration action</param>
        public static void UseHangFire(
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
                throw new InvalidOperationException("");
            }

            JobStorage.Current = configuration.Storage;

            foreach (var filter in configuration.Filters)
            {
                GlobalJobFilters.Filters.Add(filter);
            }

            foreach (var server in configuration.Servers)
            {
                app.RunHangFireServer(server());
            }

            app.MapHangFireDashboard(configuration.DashboardPath, configuration.AuthorizationFilters);
        }
    }
}
