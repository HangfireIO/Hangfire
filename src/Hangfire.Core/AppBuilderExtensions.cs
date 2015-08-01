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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Server;
using Owin;
using Microsoft.Owin;
using Microsoft.Owin.Infrastructure;

namespace Hangfire
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using MidFunc = Func<
        Func<IDictionary<string, object>, Task>,
        Func<IDictionary<string, object>, Task>
        >;
    using BuildFunc = Action<
        Func<
            IDictionary<string, object>,
            Func<
                Func<IDictionary<string, object>, Task>,
                Func<IDictionary<string, object>, Task>
        >>>;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class AppBuilderExtensions
    {
        // Prevent GC to collect background servers in hosts that do not
        // support shutdown notifications.
        private static readonly ConcurrentBag<BackgroundJobServer> Servers
            = new ConcurrentBag<BackgroundJobServer>();

        public static IAppBuilder UseHangfireServer([NotNull] this IAppBuilder builder)
        {
            return builder.UseHangfireServer(new BackgroundJobServerOptions());
        }

        public static IAppBuilder UseHangfireServer(
            [NotNull] this IAppBuilder builder, 
            [NotNull] params IBackgroundProcess[] additionalProcesses)
        {
            return builder.UseHangfireServer(JobStorage.Current, new BackgroundJobServerOptions(), additionalProcesses);
        }

        public static IAppBuilder UseHangfireServer(
            [NotNull] this IAppBuilder builder,
            [NotNull] BackgroundJobServerOptions options)
        {
            return builder.UseHangfireServer(options, JobStorage.Current);
        }

        public static IAppBuilder UseHangfireServer(
            [NotNull] this IAppBuilder builder,
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] params IBackgroundProcess[] additionalProcesses)
        {
            return builder.UseHangfireServer(JobStorage.Current, options, additionalProcesses);
        }

        public static IAppBuilder UseHangfireServer(
            [NotNull] this IAppBuilder builder,
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] JobStorage storage)
        {
            return builder.UseHangfireServer(storage, options, new IBackgroundProcess[0]);
        }

        public static IAppBuilder UseHangfireServer(
            [NotNull] this IAppBuilder builder,
            [NotNull] JobStorage storage,
            [NotNull] BackgroundJobServerOptions options, 
            [NotNull] params IBackgroundProcess[] additionalProcesses)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (storage == null) throw new ArgumentNullException("storage");
            if (options == null) throw new ArgumentNullException("options");
            if (additionalProcesses == null) throw new ArgumentNullException(nameof(additionalProcesses));

            var server = new BackgroundJobServer(options, storage, additionalProcesses);
            Servers.Add(server);

            var context = new OwinContext(builder.Properties);
            var token = context.Get<CancellationToken>("host.OnAppDisposing");
            if (token == default(CancellationToken))
            {
                // https://github.com/owin/owin/issues/27
                token = context.Get<CancellationToken>("server.OnDispose");
            }

            if (token == default(CancellationToken))
            {
                throw new InvalidOperationException("Current OWIN environment does not contain an instance of the `CancellationToken` class under `host.OnAppDisposing` key.");
            }

            token.Register(server.Dispose);

            return builder;
        }

        public static IAppBuilder UseHangfireDashboard([NotNull] this IAppBuilder builder)
        {
            return builder.UseHangfireDashboard("/hangfire");
        }

        public static IAppBuilder UseHangfireDashboard(
            [NotNull] this IAppBuilder builder,
            [NotNull] string pathMatch)
        {
            return builder.UseHangfireDashboard(pathMatch, new DashboardOptions());
        }

        public static IAppBuilder UseHangfireDashboard(
            [NotNull] this IAppBuilder builder,
            [NotNull] string pathMatch,
            [NotNull] DashboardOptions options)
        {
            return builder.UseHangfireDashboard(pathMatch, options, JobStorage.Current);
        }

        public static IAppBuilder UseHangfireDashboard(
            [NotNull] this IAppBuilder builder,
            [NotNull] string pathMatch,
            [NotNull] DashboardOptions options,
            [NotNull] JobStorage storage)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (pathMatch == null) throw new ArgumentNullException("pathMatch");
            if (options == null) throw new ArgumentNullException("options");
            if (storage == null) throw new ArgumentNullException("storage");

            SignatureConversions.AddConversions(builder);

            builder.Map(pathMatch, subApp => subApp
                .UseOwin()
                .UseHangfireDashboard(options, storage, DashboardRoutes.Routes));

            return builder;
        }

        private static BuildFunc UseOwin(this IAppBuilder builder)
        {
            return middleware => builder.Use(middleware(builder.Properties));
        }
    }
}
