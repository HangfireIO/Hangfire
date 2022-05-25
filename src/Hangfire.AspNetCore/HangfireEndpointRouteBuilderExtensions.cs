// This file is part of Hangfire. Copyright © 2019 Hangfire OÜ.
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

#if NETCOREAPP3_0
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Hangfire
{
    public static class HangfireEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapHangfireDashboard(
            [NotNull] this IEndpointRouteBuilder endpoints,
            [CanBeNull] DashboardOptions options = null,
            [CanBeNull] JobStorage storage = null)
        {
            return MapHangfireDashboard(endpoints, "/hangfire", options, storage);
        }

        public static IEndpointConventionBuilder MapHangfireDashboard(
            [NotNull] this IEndpointRouteBuilder endpoints,
            [NotNull] string pattern,
            [CanBeNull] DashboardOptions options = null,
            [CanBeNull] JobStorage storage = null)
        {
            if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));

            var app = endpoints.CreateApplicationBuilder();

            HangfireServiceCollectionExtensions.ThrowIfNotConfigured(app.ApplicationServices);

            var services = app.ApplicationServices;

            storage = storage ?? services.GetRequiredService<JobStorage>();
            options = options ?? services.GetService<DashboardOptions>() ?? new DashboardOptions();
            options.TimeZoneResolver = options.TimeZoneResolver ?? services.GetService<ITimeZoneResolver>();

            var routes = app.ApplicationServices.GetRequiredService<Dashboard.RouteCollection>();

            var pipeline = app
                .UsePathBase(pattern)
                .UseMiddleware<AspNetCoreDashboardMiddleware>(storage, options, routes)
                .Build();

            return endpoints.Map(pattern + "/{**path}", pipeline);
        }

        public static IEndpointConventionBuilder MapHangfireDashboardWithAuthorizationPolicy(
            [NotNull] this IEndpointRouteBuilder endpoints,
            [NotNull] string authorizationPolicyName,
            [NotNull] string pattern = "/hangfire",
            [CanBeNull] DashboardOptions options = null,
            [CanBeNull] JobStorage storage = null)
        {
            if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));
            if (authorizationPolicyName == null) throw new ArgumentNullException(nameof(authorizationPolicyName));

            options = options ?? new DashboardOptions();

            // We don't require the default LocalRequestsOnlyAuthorizationFilter since we provide our own policy
            options.Authorization = Enumerable.Empty<IDashboardAuthorizationFilter>();
            options.AsyncAuthorization = Enumerable.Empty<IDashboardAsyncAuthorizationFilter>();

            return endpoints
                .MapHangfireDashboard(pattern, options, storage)
                .RequireAuthorization(authorizationPolicyName);
        }
    }
}

#endif
