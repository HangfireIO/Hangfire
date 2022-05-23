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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Dashboard.Owin;
using Hangfire.Logging;
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
    /// Provides extension methods for the <c>IAppBuilder</c> interface
    /// defined in the <see href="https://www.nuget.org/packages/Owin/">Owin</see> 
    /// NuGet package to simplify the integration with OWIN applications.
    /// </summary>
    /// 
    /// <remarks>
    /// 
    /// <para>This class simplifies Hangfire configuration in OWIN applications,
    /// please read <see href="http://www.asp.net/aspnet/overview/owin-and-katana/getting-started-with-owin-and-katana">
    /// Getting Started with OWIN and Katana</see> if you aren't familiar with OWIN
    /// and/or don't know what is the <c>Startup</c> class.
    /// </para>
    /// 
    /// <para>The methods of this class should be called from OWIN's <c>Startup</c> 
    /// class.</para>
    /// 
    /// <h3>UseHangfireDashboard</h3>
    /// <para>Dashboard UI contains pages that allow you to monitor almost every
    /// aspect of background processing. It is exposed as an OWIN middleware that 
    /// intercepts requests to the given path.</para>
    /// <para>OWIN implementation of Dashboard UI allows to use it outside of web
    /// applications, including console applications and Windows Services.</para>
    /// <note type="important">
    /// By default, an access to the Dashboard UI is restricted <b>only to local
    /// requests</b> for security reasons. Before publishing a project to
    /// production, make sure you still have access to the Dashboard UI by using the
    /// <see href="https://www.nuget.org/packages/Hangfire.Dashboard.Authorization/">
    /// Hangfire.Dashboard.Authorization</see> package.</note>
    /// 
    /// <h3>UseHangfireServer</h3>
    /// <para>In addition to creation of a new instance of the <see cref="BackgroundJobServer"/> 
    /// class, these methods also register the call to its <see cref="BackgroundJobServer.Dispose"/> 
    /// method on application shutdown. This is done via registering a callback on the corresponding 
    /// <see cref="CancellationToken"/> from OWIN environment (<c>"host.OnAppDisposing"</c> or 
    /// <c>"server.OnDispose"</c> keys).</para>
    /// <para>This enables <i>graceful shutdown</i> feature for background jobs and background processes
    /// without any additional configuration.</para>
    /// <para>Please see <see cref="BackgroundJobServer"/> for more details regarding
    /// background processing.</para>
    /// </remarks>
    /// 
    /// <example>
    /// <h3>Basic Configuration</h3> 
    /// <para>Basic setup in an OWIN application looks like the following example. Please note
    /// that job storage should be configured before using the methods of this class.</para>
    /// 
    /// <code lang="cs" source="..\Samples\AppBuilderExtensions.cs" region="Basic Setup" />
    /// 
    /// <h3>Adding Dashboard Only</h3>
    /// <para>If you want to install dashboard without starting a background job server, for example,
    /// to process background jobs outside of your web application, call only the
    /// <see cref="O:Hangfire.AppBuilderExtensions.UseHangfireDashboard"/>.</para>
    /// 
    /// <code lang="cs" source="..\Samples\AppBuilderExtensions.cs" region="Dashboard Only" />
    /// 
    /// <h3>Change Dashboard Path</h3>
    /// <para>By default, you can access Dashboard UI by hitting the <i>http(s)://&lt;app&gt;/hangfire</i>
    /// URL, however you can change it as in the following example.</para>
    /// 
    /// <code lang="cs" source="..\Samples\AppBuilderExtensions.cs" region="Change Dashboard Path" />
    /// 
    /// <h3>Configuring Authorization</h3>
    /// <para>The following example demonstrates how to change default local-requests-only
    /// authorization for Dashboard UI.</para>
    /// 
    /// <code lang="cs" source="..\Samples\AppBuilderExtensions.cs" region="Configuring Authorization" />
    /// 
    /// <h3>Changing Application Path</h3>
    /// <para>Have you seen the <i>Back to site</i> button in the Dashboard? By default it leads
    /// you to the root of your site, but you can configure the behavior.</para>
    /// 
    /// <code lang="cs" source="..\Samples\AppBuilderExtensions.cs" region="Change Application Path" />
    /// 
    /// <h3>Multiple Dashboards</h3>
    /// <para>The following example demonstrates adding multiple Dashboard UI endpoints. This may
    /// be useful when you are using multiple shards for your background processing needs.</para>
    /// 
    /// <code lang="cs" source="..\Samples\AppBuilderExtensions.cs" region="Multiple Dashboards" />
    /// 
    /// </example>
    /// 
    /// <seealso cref="BackgroundJobServer"/>
    /// <seealso cref="Hangfire.Dashboard"/>
    /// <seealso href="https://www.nuget.org/packages/Hangfire.Dashboard.Authorization/">
    /// Hangfire.Dashboard.Authorization Package
    /// </seealso>
    /// 
    /// <threadsafety static="true" instance="false" />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class AppBuilderExtensions
    {
        // Prevent GC to collect background processing servers in hosts that do
        // not support shutdown notifications. Dictionary is used as a Set.
        private static readonly ConcurrentDictionary<BackgroundJobServer, object> Servers
            = new ConcurrentDictionary<BackgroundJobServer, object>();

        /// <summary>
        /// Creates a new instance of the <see cref="BackgroundJobServer"/> class
        /// with default options and <see cref="JobStorage.Current"/> storage and
        /// registers its disposal on application shutdown.
        /// </summary>
        /// <param name="builder">OWIN application builder.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// OWIN environment does not contain the application shutdown cancellation token.
        /// </exception>
        /// 
        /// <remarks>
        /// Please see <see cref="AppBuilderExtensions"/> for details and examples.
        /// </remarks>
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
        /// <exception cref="InvalidOperationException">
        /// OWIN environment does not contain the application shutdown cancellation token.
        /// </exception>
        /// 
        /// <remarks>
        /// Please see <see cref="AppBuilderExtensions"/> for details and examples.
        /// </remarks>
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
        /// <exception cref="InvalidOperationException">
        /// OWIN environment does not contain the application shutdown cancellation token.
        /// </exception>
        /// 
        /// <remarks>
        /// Please see <see cref="AppBuilderExtensions"/> for details and examples.
        /// </remarks>
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
        /// <exception cref="InvalidOperationException">
        /// OWIN environment does not contain the application shutdown cancellation token.
        /// </exception>
        /// 
        /// <remarks>
        /// Please see <see cref="AppBuilderExtensions"/> for details and examples.
        /// </remarks>
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
        /// <exception cref="InvalidOperationException">
        /// OWIN environment does not contain the application shutdown cancellation token.
        /// </exception>
        /// 
        /// <remarks>
        /// Please see <see cref="AppBuilderExtensions"/> for details and examples.
        /// </remarks>
        public static IAppBuilder UseHangfireServer(
            [NotNull] this IAppBuilder builder,
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] JobStorage storage)
        {
            return builder.UseHangfireServer(storage, options);
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
        /// <exception cref="InvalidOperationException">
        /// OWIN environment does not contain the application shutdown cancellation token.
        /// </exception>
        /// 
        /// <remarks>
        /// Please see <see cref="AppBuilderExtensions"/> for details and examples.
        /// </remarks>
        public static IAppBuilder UseHangfireServer(
            [NotNull] this IAppBuilder builder,
            [NotNull] JobStorage storage,
            [NotNull] BackgroundJobServerOptions options, 
            [NotNull] params IBackgroundProcess[] additionalProcesses)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (additionalProcesses == null) throw new ArgumentNullException(nameof(additionalProcesses));

            var server = new BackgroundJobServer(options, storage, additionalProcesses);
            Servers.TryAdd(server, null);

            var context = new OwinContext(builder.Properties);
            var token = context.Get<CancellationToken>("host.OnAppDisposing");
            if (token == default(CancellationToken))
            {
                // https://github.com/owin/owin/issues/27
                token = context.Get<CancellationToken>("server.OnDispose");
            }

            if (token == default(CancellationToken))
            {
                throw new InvalidOperationException(
                    "Current OWIN environment does not contain an instance of the `CancellationToken` class neither under `host.OnAppDisposing`, nor `server.OnDispose` key.\r\n"
                    + "Please use another OWIN host or create an instance of the `BackgroundJobServer` class manually.");
            }

            token.Register(OnAppDisposing, server);
            return builder;
        }

        private static void OnAppDisposing(object state)
        {
            var logger = LogProvider.GetLogger(typeof(AppBuilderExtensions));
            logger.Info("Web application is shutting down via OWIN's host.OnAppDisposing callback.");
            ((IDisposable) state).Dispose();
            var server = state as BackgroundJobServer;
            if (server != null)
                Servers.TryRemove(server, out _);
        }

        /// <summary>
        /// Adds Dashboard UI middleware to the OWIN request processing pipeline under 
        /// the <c>/hangfire</c> path, for the <see cref="JobStorage.Current"/> storage.
        /// </summary>
        /// <param name="builder">OWIN application builder.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        /// 
        /// <remarks>
        /// Please see <see cref="AppBuilderExtensions"/> for details and examples.
        /// </remarks>
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
        /// 
        /// <remarks>
        /// Please see <see cref="AppBuilderExtensions"/> for details and examples.
        /// </remarks>
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
        /// 
        /// <remarks>
        /// Please see <see cref="AppBuilderExtensions"/> for details and examples.
        /// </remarks>
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
        /// 
        /// <remarks>
        /// Please see <see cref="AppBuilderExtensions"/> for details and examples.
        /// </remarks>
        public static IAppBuilder UseHangfireDashboard(
            [NotNull] this IAppBuilder builder,
            [NotNull] string pathMatch,
            [NotNull] DashboardOptions options,
            [NotNull] JobStorage storage)
        {
            return builder.UseHangfireDashboard(pathMatch, options, storage, null);
        }

        /// <summary>
        /// Adds Dashboard UI middleware to the OWIN request processing pipeline with the
        /// specified parameters and antiforgery service.
        /// </summary>
        /// <param name="builder">OWIN application builder.</param>
        /// <param name="pathMatch">Path prefix for middleware to use, e.g. "/hangfire".</param>
        /// <param name="options">Options for Dashboard UI.</param>
        /// <param name="storage">Job storage to use by Dashboard IO.</param>
        /// <param name="antiforgery">Antiforgery service.</param>
        /// 
        /// <exception cref="ArgumentNullException"><paramref name="builder"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="pathMatch"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="storage"/> is null.</exception>
        /// 
        /// <remarks>
        /// Please see <see cref="AppBuilderExtensions"/> for details and examples.
        /// </remarks>
        public static IAppBuilder UseHangfireDashboard(
                [NotNull] this IAppBuilder builder,
                [NotNull] string pathMatch,
                [NotNull] DashboardOptions options,
                [NotNull] JobStorage storage,
                [CanBeNull] IOwinDashboardAntiforgery antiforgery)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (pathMatch == null) throw new ArgumentNullException(nameof(pathMatch));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            SignatureConversions.AddConversions(builder);

            builder.Map(pathMatch, subApp => subApp
                .UseOwin()
                .UseHangfireDashboard(options, storage, DashboardRoutes.Routes, antiforgery));

            return builder;
        }

        private static BuildFunc UseOwin(this IAppBuilder builder)
        {
            return middleware => builder.Use(middleware(builder.Properties));
        }
    }
}
