#if NETCOREAPP3_0

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
    }
}

#endif
