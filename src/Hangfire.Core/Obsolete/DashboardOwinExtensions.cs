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
using System.Collections.Generic;
using Hangfire.Annotations;
using Microsoft.Owin.Infrastructure;
using Owin;

// ReSharper disable once CheckNamespace
namespace Hangfire.Dashboard
{
    /// <exclude />
    [Obsolete("Please use `IAppBuilder.UseHangfireDashboard` OWIN extension method instead. Will be removed in version 2.0.0.")]
    public static class DashboardOwinExtensions
    {
        internal static readonly IAuthorizationFilter[] DefaultAuthorizationFilters =
        {
            new LocalRequestsOnlyAuthorizationFilter()
        };

        internal static readonly string DefaultDashboardPath = "/hangfire";
        internal static readonly string DefaultAppPath = "/";

        /// <summary>
        /// Maps dashboard to the app builder pipeline at "/hangfire"
        /// with authorization filter that blocks all remote requests
        /// and <see cref="JobStorage.Current"/> storage instance.
        /// </summary>
        /// <param name="app">The app builder</param>
        [Obsolete("Please use `IAppBuilder.UseHangfireDashboard` OWIN extension method instead. Will be removed in version 2.0.0.")]
        public static void MapHangfireDashboard(this IAppBuilder app)
        {
            MapHangfireDashboard(app, DefaultDashboardPath, DefaultAppPath);
        }

        /// <summary>
        /// Maps dashboard to the app builder pipeline at the specified
        /// path with authorization filter that blocks all remote requests
        /// and <see cref="JobStorage.Current"/> storage instance.
        /// </summary>
        /// <param name="app">The app builder</param>
        /// <param name="dashboardPath">The path to map dashboard</param>
        [Obsolete("Please use `IAppBuilder.UseHangfireDashboard` OWIN extension method instead. Will be removed in version 2.0.0.")]
        public static void MapHangfireDashboard(
            this IAppBuilder app,
            string dashboardPath)
        {
            MapHangfireDashboard(app, dashboardPath, DefaultAppPath, DefaultAuthorizationFilters);
        }

        /// <summary>
        /// Maps dashboard to the app builder pipeline at the specified
        /// path with authorization filter that blocks all remote requests
        /// and <see cref="JobStorage.Current"/> storage instance.
        /// </summary>
        /// <param name="app">The app builder</param>
        /// <param name="dashboardPath">The path to map dashboard</param>
        /// <param name="appPath">The application path on Back To Site link. Pass null in order to hide the Back To Site link.</param>
        [Obsolete("Please use `IAppBuilder.UseHangfireDashboard` OWIN extension method instead. Will be removed in version 2.0.0.")]
        public static void MapHangfireDashboard(
            this IAppBuilder app,
            string dashboardPath,
            string appPath)
        {
            MapHangfireDashboard(app, dashboardPath, appPath, DefaultAuthorizationFilters);
        }

        /// <summary>
        /// Maps dashboard to the app builder pipeline at the specified
        /// path with given authorization filters that apply to any request
        /// and <see cref="JobStorage.Current"/> storage instance.
        /// </summary>
        /// <param name="app">The app builder</param>
        /// <param name="dashboardPath">The path to map dashboard</param>
        /// <param name="appPath">The application path on Back To Site link</param>
        /// <param name="authorizationFilters">Array of authorization filters</param>
        [Obsolete("Please use `IAppBuilder.UseHangfireDashboard` OWIN extension method instead. Will be removed in version 2.0.0.")]
        public static void MapHangfireDashboard(
            this IAppBuilder app, 
            string dashboardPath,
            string appPath,
            IEnumerable<IAuthorizationFilter> authorizationFilters)
        {
            MapHangfireDashboard(app, dashboardPath, appPath, authorizationFilters, JobStorage.Current);
        }

        /// <summary>
        /// Maps dashboard to the app builder pipeline at the specified path
        /// with given authorization filters that apply to any request and
        /// storage instance that is used to query the information.
        /// </summary>
        /// <param name="app">The app builder</param>
        /// <param name="dashboardPath">The path to map dashboard</param>
        /// <param name="appPath">The application path on Back To Site link</param>
        /// <param name="authorizationFilters">Array of authorization filters</param>
        /// <param name="storage">The storage instance</param>
        [Obsolete("Please use `IAppBuilder.UseHangfireDashboard` OWIN extension method instead. Will be removed in version 2.0.0.")]
        public static void MapHangfireDashboard(
            [NotNull] this IAppBuilder app,
            string dashboardPath,
            string appPath,
            IEnumerable<IAuthorizationFilter> authorizationFilters,
            JobStorage storage)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            SignatureConversions.AddConversions(app);

            app.Map(dashboardPath, subApp => subApp.Use<DashboardMiddleware>(
                appPath,
                storage,
                DashboardRoutes.Routes,
                authorizationFilters));
        }
    }
}
