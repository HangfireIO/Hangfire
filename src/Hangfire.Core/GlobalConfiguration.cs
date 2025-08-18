// This file is part of Hangfire. Copyright © 2015 Hangfire OÜ.
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

// ReSharper disable InconsistentNaming

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Logging;

namespace Hangfire
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Public API, can not touch in minor versions.")]
    public enum CompatibilityLevel
    {
        Version_110 = 110,
        Version_170 = 170,
        Version_180 = 180,
        Version_190 = 190,
    }

    public class GlobalConfiguration : IGlobalConfiguration
    {
        private static int _compatibilityLevel = (int)CompatibilityLevel.Version_110;
        private readonly ConcurrentDictionary<Type, KeyValuePair<Func<object, object>, object>> _services;

        [NotNull]
        public static GlobalConfiguration Configuration
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        } = new GlobalConfiguration();

        internal static CompatibilityLevel CompatibilityLevel
        {
            get => (CompatibilityLevel)Volatile.Read(ref _compatibilityLevel);
            set => Volatile.Write(ref _compatibilityLevel, (int)value);
        }

        internal static bool HasCompatibilityLevel(CompatibilityLevel level)
        {
            return CompatibilityLevel >= level;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod", Justification = "Just reassuring that required types are used, regardless of return values.")]
        public GlobalConfiguration()
        {
            _services = new ConcurrentDictionary<Type, KeyValuePair<Func<object, object>, object>>();

            RegisterService<ILogProvider>(LogProvider.ResolveLogProvider());
            RegisterService<JobActivator>(new JobActivator());
            RegisterUninitializedStorage();
        }

        public GlobalConfiguration(GlobalConfiguration configuration)
        {
            _services = new ConcurrentDictionary<Type, KeyValuePair<Func<object, object>, object>>(configuration._services);
        }

        public void RegisterService<TService>([NotNull] TService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            _services[typeof(TService)] = new KeyValuePair<Func<object, object>, object>(static state => state, service);
        }

        public void RegisterService<TService>([NotNull] Func<TService> serviceFactory)
        {
            if (serviceFactory == null) throw new ArgumentNullException(nameof(serviceFactory));

            _services[typeof(TService)] = new KeyValuePair<Func<object, object>, object>(
                static state => ((Func<TService>)state)()!,
                serviceFactory);
        }

        [NotNull]
        public T ResolveService<T>()
        {
            if (!_services.TryGetValue(typeof(T), out var service))
            {
                throw new InvalidOperationException($"Service of type '{typeof(T).FullName}' has not been registered.");
            }

            return (T)service.Key(service.Value);
        }

        internal void RegisterUninitializedStorage()
        {
            RegisterService<JobStorage>(static () =>
            {
                throw new InvalidOperationException(
                    "Current JobStorage instance has not been initialized yet. You must set it before using Hangfire Client or Server API. " +
#if NET45 || NET46
                    "For NET Framework applications please use GlobalConfiguration.Configuration.UseXXXStorage method, where XXX is the storage type, like `UseSqlServerStorage`."
#else
                    "For .NET Core applications please call the `IServiceCollection.AddHangfire` extension method from Hangfire.NetCore or Hangfire.AspNetCore package depending on your application type when configuring the services and ensure service-based APIs are used instead of static ones, like `IBackgroundJobClient` instead of `BackgroundJob` and `IRecurringJobManager` instead of `RecurringJob`."
#endif
                );
            });
        }
    }

    public static class CompatibilityLevelExtensions
    {
        public static IGlobalConfiguration SetDataCompatibilityLevel(
            [NotNull] this IGlobalConfiguration configuration,
            CompatibilityLevel compatibilityLevel)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

#if !NETSTANDARD1_3
            if (!Enum.IsDefined(typeof(CompatibilityLevel), compatibilityLevel))
                throw new InvalidEnumArgumentException(nameof(compatibilityLevel), (int) compatibilityLevel,
                    typeof(CompatibilityLevel));
#endif

            GlobalConfiguration.CompatibilityLevel = compatibilityLevel;

            return configuration;
        }
    }
}
