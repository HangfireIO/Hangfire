// This file is part of Hangfire.
// Copyright © 2016 Sergey Odinokov.
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
using Hangfire.AspNetCore;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Dashboard;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
#if NETCOREAPP3_0 || NETSTANDARD2_0 || NET461
using Microsoft.Extensions.Hosting;
#endif

namespace Hangfire
{
    public static class HangfireServiceCollectionExtensions
    {
        public static IServiceCollection AddHangfire(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<IGlobalConfiguration> configuration)
        {
            return AddHangfire(services, (provider, config) => configuration(config));
        }

        public static IServiceCollection AddHangfire(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<IServiceProvider, IGlobalConfiguration> configuration)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            services.TryAddSingletonChecked(_ => JobStorage.Current);
            services.TryAddSingletonChecked(_ => JobActivator.Current);

            services.TryAddSingleton(_ => DashboardRoutes.Routes);
            services.TryAddSingleton<IJobFilterProvider>(_ => JobFilterProviders.Providers);
            services.TryAddSingleton<ITimeZoneResolver>(_ => new DefaultTimeZoneResolver());

            services.TryAddSingletonChecked<IBackgroundJobClient>(x =>
            {
                if (GetInternalServices(x, out var factory, out var stateChanger, out _))
                {
                    return new BackgroundJobClient(x.GetRequiredService<JobStorage>(), factory, stateChanger);
                }

                return new BackgroundJobClient(
                    x.GetRequiredService<JobStorage>(),
                    x.GetRequiredService<IJobFilterProvider>());
            });

            services.TryAddSingletonChecked<IRecurringJobManager>(x =>
            {
                if (GetInternalServices(x, out var factory, out _, out _))
                {
                    return new RecurringJobManager(
                        x.GetRequiredService<JobStorage>(),
                        factory,
                        x.GetRequiredService<ITimeZoneResolver>());
                }

                return new RecurringJobManager(
                    x.GetRequiredService<JobStorage>(),
                    x.GetRequiredService<IJobFilterProvider>(),
                    x.GetRequiredService<ITimeZoneResolver>());
            });


            // IGlobalConfiguration serves as a marker indicating that Hangfire's services 
            // were added to the service container (checked by IApplicationBuilder extensions).
            // 
            // Being a singleton, it also guarantees that the configuration callback will be 
            // executed just once upon initialization, so there's no need to double-check that.
            // 
            // It should never be replaced by another implementation !!!
            // AddSingleton() will throw an exception if it was already registered

            services.AddSingleton<IGlobalConfiguration>(serviceProvider =>
            {
                var configurationInstance = GlobalConfiguration.Configuration;

                // init defaults for log provider and job activator
                // they may be overwritten by the configuration callback later

                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                if (loggerFactory != null)
                {
                    configurationInstance.UseLogProvider(new AspNetCoreLogProvider(loggerFactory));
                }

                var scopeFactory = serviceProvider.GetService<IServiceScopeFactory>();
                if (scopeFactory != null)
                {
                    configurationInstance.UseActivator(new AspNetCoreJobActivator(scopeFactory));
                }

                // do configuration inside callback

                configuration(serviceProvider, configurationInstance);
                
                return configurationInstance;
            });
            
            return services;
        }

#if NETCOREAPP3_0 || NETSTANDARD2_0 || NET461
        public static IServiceCollection AddHangfireServer(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<BackgroundJobServerOptions> optionsAction)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (optionsAction == null) throw new ArgumentNullException(nameof(optionsAction));

            services.AddTransient<IHostedService, BackgroundJobServerHostedService>(provider =>
            {
                var options = new BackgroundJobServerOptions();
                optionsAction(options);

                return CreateBackgroundJobServerHostedService(provider, options);
            });

            return services;
        }

        public static IServiceCollection AddHangfireServer([NotNull] this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddTransient<IHostedService, BackgroundJobServerHostedService>(provider =>
            {
                var options = provider.GetService<BackgroundJobServerOptions>() ?? new BackgroundJobServerOptions();
                return CreateBackgroundJobServerHostedService(provider, options);
            });

            return services;
        }

        private static BackgroundJobServerHostedService CreateBackgroundJobServerHostedService(
            IServiceProvider provider,
            BackgroundJobServerOptions options)
        {
            ThrowIfNotConfigured(provider);

            var storage = provider.GetService<JobStorage>() ?? JobStorage.Current;
            var additionalProcesses = provider.GetServices<IBackgroundProcess>();

            options.Activator = options.Activator ?? provider.GetService<JobActivator>();
            options.FilterProvider = options.FilterProvider ?? provider.GetService<IJobFilterProvider>();
            options.TimeZoneResolver = options.TimeZoneResolver ?? provider.GetService<ITimeZoneResolver>();

            GetInternalServices(provider, out var factory, out var stateChanger, out var performer);

#pragma warning disable 618
            return new BackgroundJobServerHostedService(
#pragma warning restore 618
                storage, options, additionalProcesses, factory, performer, stateChanger);
        }
#endif

        internal static bool GetInternalServices(
            IServiceProvider provider,
            out IBackgroundJobFactory factory,
            out IBackgroundJobStateChanger stateChanger,
            out IBackgroundJobPerformer performer)
        {
            factory = provider.GetService<IBackgroundJobFactory>();
            performer = provider.GetService<IBackgroundJobPerformer>();
            stateChanger = provider.GetService<IBackgroundJobStateChanger>();

            if (factory != null && performer != null && stateChanger != null)
            {
                return true;
            }

            factory = null;
            performer = null;
            stateChanger = null;

            return false;
        }

        private static void TryAddSingletonChecked<T>(
            [NotNull] this IServiceCollection serviceCollection, 
            [NotNull] Func<IServiceProvider, T> implementationFactory)
            where T : class
        {
            serviceCollection.TryAddSingleton<T>(serviceProvider =>
            {
                if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

                // ensure the configuration was performed
                serviceProvider.GetRequiredService<IGlobalConfiguration>();

                return implementationFactory(serviceProvider);
            });
        }

        internal static void ThrowIfNotConfigured(IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetService<IGlobalConfiguration>();
            if (configuration == null)
            {
                throw new InvalidOperationException(
                    "Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddHangfire' inside the call to 'ConfigureServices(...)' in the application startup code.");
            }
        }
    }
}
