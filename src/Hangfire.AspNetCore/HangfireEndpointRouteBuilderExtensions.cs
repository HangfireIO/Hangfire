#if NETCOREAPP3_0
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;

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
            [CanBeNull] DashboardOptions options = null,
            [CanBeNull] JobStorage storage = null)
        {
            return MapHangfireDashboardWithAuthorizationPolicy(endpoints, authorizationPolicyName, "/hangfire", options, storage);
        }

        public static IEndpointConventionBuilder MapHangfireDashboardWithAuthorizationPolicy(
            [NotNull] this IEndpointRouteBuilder endpoints,
            [NotNull] string authorizationPolicyName,
            [NotNull] string pattern,
            [CanBeNull] DashboardOptions options = null,
            [CanBeNull] JobStorage storage = null)
        {
            if (options == null)
            {
                options = new DashboardOptions()
                {
                    Authorization = new List<IDashboardAuthorizationFilter>() // We don't require the default LocalHost only filter since we provide our own policy
                };
            }

            return endpoints
                .MapHangfireDashboard(pattern, options, storage)
                .RequireAuthorization(authorizationPolicyName);
        }
    }
}

#endif
