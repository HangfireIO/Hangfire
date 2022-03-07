// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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
