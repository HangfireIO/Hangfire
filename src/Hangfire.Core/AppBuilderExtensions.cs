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
    using BuildFunc = Action<
        Func<
            IDictionary<string, object>,
            Func<
                Func<IDictionary<string, object>, Task>,
                Func<IDictionary<string, object>, Task>
        >>>;

    /// <summary>
    /// Provides extension methods for the <see cref="IAppBuilder"/> interface
    /// defined in the <see href="https://www.nuget.org/packages/Owin/">Owin</see> 
    /// NuGet package to simplify the integration with OWIN applications.
    /// </summary>
    /// 
    /// <threadsafety static="true" instance="false" />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class AppBuilderExtensions
    {
        // Prevent GC to collect background servers in hosts that do not
        // support shutdown notifications.
        private static readonly ConcurrentBag<BackgroundJobServer> Servers
            = new ConcurrentBag<BackgroundJobServer>();

        /// <summary>
        /// Creates a new instance of the <see cref="BackgroundJobServer"/> class
        /// with default options and <see cref="JobStorage.Current"/> storage and
        /// registers its disposal on application shutdown.
        /// </summary>
        /// <param name="builder">OWIN application builder.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        public static IAppBuilder UseHangfireServer([NotNull] this IAppBuilder builder)
        {
            return builder.UseHangfireServer(new BackgroundJobServerOptions());
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BackgroundJobServer"/> class 
        /// with the given collection of additional background processes and 
        /// <see cref="JobStorage.Current"/> storage, and registers its disposal
        /// on application shutdown.
        /// </summary>
        /// <param name="builder">OWIN application builder.</param>
        /// <param name="additionalProcesses">Collection of additional background processes.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="additionalProcesses"/> is null.</exception>
        public static IAppBuilder UseHangfireServer(
            [NotNull] this IAppBuilder builder, 
            [NotNull] params IBackgroundProcess[] additionalProcesses)
        {
            return builder.UseHangfireServer(JobStorage.Current, new BackgroundJobServerOptions(), additionalProcesses);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BackgroundJobServer"/> class
        /// with the specified options and <see cref="JobStorage.Current"/> storage,
        /// and registers its disposal on application shutdown.
        /// </summary>
        /// <param name="builder">OWIN application builder.</param>
        /// <param name="options">Options for background job server.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        public static IAppBuilder UseHangfireServer(
            [NotNull] this IAppBuilder builder,
            [NotNull] BackgroundJobServerOptions options)
        {
            return builder.UseHangfireServer(options, JobStorage.Current);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BackgroundJobServer"/> class
        /// with the specified options, given collection of background processes
        /// and <see cref="JobStorage.Current"/> storage, and registers its
        /// disposal on application shutdown.
        /// </summary>
        /// <param name="builder">OWIN application builder.</param>
        /// <param name="options">Options for background job server.</param>
        /// <param name="additionalProcesses">Collection of additional background processes.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="additionalProcesses"/> is null.</exception>
        public static IAppBuilder UseHangfireServer(
            [NotNull] this IAppBuilder builder,
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] params IBackgroundProcess[] additionalProcesses)
        {
            return builder.UseHangfireServer(JobStorage.Current, options, additionalProcesses);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BackgroundJobServer"/> class
        /// with the given options and specified storage, and registers its disposal
        /// on application shutdown.
        /// </summary>
        /// <param name="builder">OWIN application builder.</param>
        /// <param name="options">Options for background job server.</param>
        /// <param name="storage">Storage to use by background job server.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="storage"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        public static IAppBuilder UseHangfireServer(
            [NotNull] this IAppBuilder builder,
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] JobStorage storage)
        {
            return builder.UseHangfireServer(storage, options, new IBackgroundProcess[0]);
        }

        /// <summary>
        /// Starts a new instance of the <see cref="BackgroundJobServer"/> class with
        /// the given arguments, and registers its disposal on application shutdown.
        /// </summary>
        /// 
        /// <param name="builder">OWIN application builder.</param>
        /// <param name="storage">Storage to use by background job server.</param>
        /// <param name="options">Options for background job server.</param>
        /// <param name="additionalProcesses">Collection of additional background processes.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="storage"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="additionalProcesses"/> is null.</exception>
        public static IAppBuilder UseHangfireServer(
            [NotNull] this IAppBuilder builder,
            [NotNull] JobStorage storage,
            [NotNull] BackgroundJobServerOptions options, 
            [NotNull] params IBackgroundProcess[] additionalProcesses)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (storage == null) throw new ArgumentNullException("storage");
            if (options == null) throw new ArgumentNullException("options");
            if (additionalProcesses == null) throw new ArgumentNullException("additionalProcesses");

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

        /// <summary>
        /// Adds Dashboard UI middleware to the OWIN request processing pipeline under 
        /// the <c>/hangfire</c> path, for the <see cref="JobStorage.Current"/> storage.
        /// </summary>
        /// <param name="builder">OWIN application builder.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        public static IAppBuilder UseHangfireDashboard([NotNull] this IAppBuilder builder)
        {
            return builder.UseHangfireDashboard("/hangfire");
        }

        /// <summary>
        /// Adds Dashboard UI middleware to the OWIN request processing pipeline under
        /// the given path, for the <see cref="JobStorage.Current"/> storage.
        /// </summary>
        /// <param name="builder">OWIN application builder.</param>
        /// <param name="pathMatch">Path prefix for middleware to use, e.g. "/hangfire".</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="pathMatch"/> is null.</exception>
        public static IAppBuilder UseHangfireDashboard(
            [NotNull] this IAppBuilder builder,
            [NotNull] string pathMatch)
        {
            return builder.UseHangfireDashboard(pathMatch, new DashboardOptions());
        }

        /// <summary>
        /// Adds Dashboard UI middleware to the OWIN request processing pipeline under
        /// the specified path and the given options, for the <see cref="JobStorage.Current"/>
        /// storage.
        /// </summary>
        /// <param name="builder">OWIN application builder.</param>
        /// <param name="pathMatch">Path prefix for middleware to use, e.g. "/hangfire".</param>
        /// <param name="options">Options for Dashboard UI.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="pathMatch"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        public static IAppBuilder UseHangfireDashboard(
            [NotNull] this IAppBuilder builder,
            [NotNull] string pathMatch,
            [NotNull] DashboardOptions options)
        {
            return builder.UseHangfireDashboard(pathMatch, options, JobStorage.Current);
        }

        /// <summary>
        /// Adds Dashboard UI middleware to the OWIN request processing pipeline with the
        /// specified parameters.
        /// </summary>
        /// <param name="builder">OWIN application builder.</param>
        /// <param name="pathMatch">Path prefix for middleware to use, e.g. "/hangfire".</param>
        /// <param name="options">Options for Dashboard UI.</param>
        /// <param name="storage">Job storage to use by Dashboard IO.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="pathMatch"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="storage"/> is null.</exception>
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
