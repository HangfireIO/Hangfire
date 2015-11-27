// This file is part of Hangfire.
// Copyright © 2015 Sergey Odinokov.
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
using System.ComponentModel;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.Dashboard.Pages;
using Hangfire.Logging;
using Hangfire.Logging.LogProviders;

namespace Hangfire
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class GlobalConfigurationExtensions
    {
        public static IGlobalConfiguration<TStorage> UseStorage<TStorage>(
            [NotNull] this IGlobalConfiguration configuration,
            [NotNull] TStorage storage)
            where TStorage : JobStorage
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            if (storage == null) throw new ArgumentNullException("storage");

            return configuration.Use(storage, x => JobStorage.Current = x);
        }

        public static IGlobalConfiguration<TActivator> UseActivator<TActivator>(
            [NotNull] this IGlobalConfiguration configuration,
            [NotNull] TActivator activator)
            where TActivator : JobActivator
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            if (activator == null) throw new ArgumentNullException("activator");

            return configuration.Use(activator, x => JobActivator.Current = x);
        }

        public static IGlobalConfiguration<TLogProvider> UseLogProvider<TLogProvider>(
            [NotNull] this IGlobalConfiguration configuration,
            [NotNull] TLogProvider provider)
            where TLogProvider : ILogProvider
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            if (provider == null) throw new ArgumentNullException("provider");
            
            return configuration.Use(provider, x => LogProvider.SetCurrentLogProvider(x));
        }

        public static IGlobalConfiguration<NLogLogProvider> UseNLogLogProvider(
            [NotNull] this IGlobalConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            return configuration.UseLogProvider(new NLogLogProvider());
        }

        public static IGlobalConfiguration<ColouredConsoleLogProvider> UseColouredConsoleLogProvider(
            [NotNull] this IGlobalConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            return configuration.UseLogProvider(new ColouredConsoleLogProvider());
        }

        public static IGlobalConfiguration<Log4NetLogProvider> UseLog4NetLogProvider(
            [NotNull] this IGlobalConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            return configuration.UseLogProvider(new Log4NetLogProvider());
        }

        public static IGlobalConfiguration<ElmahLogProvider> UseElmahLogProvider(
            [NotNull] this IGlobalConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            return configuration.UseLogProvider(new ElmahLogProvider());
        }

        public static IGlobalConfiguration<ElmahLogProvider> UseElmahLogProvider(
            [NotNull] this IGlobalConfiguration configuration,
            LogLevel minLevel)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            return configuration.UseLogProvider(new ElmahLogProvider(minLevel));
        }

        public static IGlobalConfiguration<EntLibLogProvider> UseEntLibLogProvider(
            [NotNull] this IGlobalConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            return configuration.UseLogProvider(new EntLibLogProvider());
        }

        public static IGlobalConfiguration<SerilogLogProvider> UseSerilogLogProvider(
            [NotNull] this IGlobalConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            return configuration.UseLogProvider(new SerilogLogProvider());
        }

        public static IGlobalConfiguration<LoupeLogProvider> UseLoupeLogProvider(
            [NotNull] this IGlobalConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            return configuration.UseLogProvider(new LoupeLogProvider());
        }

        public static IGlobalConfiguration<TFilter> UseFilter<TFilter>(
            [NotNull] this IGlobalConfiguration configuration, 
            [NotNull] TFilter filter)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            if (filter == null) throw new ArgumentNullException("filter");

            return configuration.Use(filter, x => GlobalJobFilters.Filters.Add(x));
        }

        public static IGlobalConfiguration UseDashboardMetric(
            [NotNull] this IGlobalConfiguration configuration,
            [NotNull] DashboardMetric metric)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            if (metric == null) throw new ArgumentNullException("metric");

            DashboardMetrics.AddMetric(metric);
            HomePage.Metrics.Add(metric);

            return configuration;
        }        

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IGlobalConfiguration<T> Use<T>(
            [NotNull] this IGlobalConfiguration configuration, T entry,
            [NotNull] Action<T> entryAction)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            entryAction(entry);

            return new ConfigurationEntry<T>(entry);
        }

        private class ConfigurationEntry<T> : IGlobalConfiguration<T>
        {
            private readonly T _entry;

            public ConfigurationEntry(T entry)
            {
                _entry = entry;
            }

            public T Entry
            {
                get { return _entry; }
            }
        }
    }
}