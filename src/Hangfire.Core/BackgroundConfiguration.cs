// This file is part of Hangfire. Copyright © 2025 Hangfire OÜ.
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
using System.Threading.Tasks;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Profiling;
using Hangfire.Server;
using Hangfire.States;

namespace Hangfire
{
    public sealed class BackgroundConfiguration : IBackgroundConfiguration
    {
        public static IBackgroundConfiguration Instance { get; } = new BackgroundConfiguration()
            .WithJobStorage(static _ => JobStorage.Current)
            .WithJobFilterProvider(static _ => JobFilterProviders.Providers)
            .With<StateMachine>(static c => new StateMachine(
                c.Resolve<IJobFilterProvider>(),
                new CoreStateMachine()))
            .WithStateMachine(static c => c.Resolve<StateMachine>())
            .WithJobFactory(static c => new BackgroundJobFactory(
                c.Resolve<IJobFilterProvider>(),
                new CoreBackgroundJobFactory(c.Resolve<IStateMachine>())))
            .WithProfiler(static _ => EmptyProfiler.Instance)
            .WithStateChanger(static c => new BackgroundJobStateChanger(c.Resolve<StateMachine>()))
            .With<IBackgroundJobPerformer>(static c => new BackgroundJobPerformer(
                c.Resolve<IJobFilterProvider>(),
                new CoreBackgroundJobPerformer(c.Resolve<JobActivator>(), c.Resolve<TaskScheduler>())))
            .With<BackgroundJobClient>(static c => new BackgroundJobClient(c))
            .With<IBackgroundJobClient>(static c => c.Resolve<BackgroundJobClient>())
            .With<IBackgroundJobClientV2>(static c => c.Resolve<BackgroundJobClient>())
            .With<RecurringJobManager>(static c => new RecurringJobManager(c))
            .With<IRecurringJobManager>(static c => c.Resolve<RecurringJobManager>())
            .With<IRecurringJobManagerV2>(static c => c.Resolve<RecurringJobManager>())
            .With<JobActivator>(static _ => JobActivator.Current)
            .With<TaskScheduler>(static _ => TaskScheduler.Default);

        private readonly Dictionary<string, Func<IBackgroundConfiguration, object>> _registrations;

        public BackgroundConfiguration()
        {
            _registrations = new Dictionary<string, Func<IBackgroundConfiguration, object>>();
        }

        private BackgroundConfiguration(Dictionary<string, Func<IBackgroundConfiguration, object>> registrations)
        {
            _registrations = registrations;
        }

        public IBackgroundConfiguration With<T>(Func<IBackgroundConfiguration, T> serviceFactory)
            where T : notnull
        {
            if (serviceFactory == null) throw new ArgumentNullException(nameof(serviceFactory));

            var newRegistrations = new Dictionary<string, Func<IBackgroundConfiguration, object>>(_registrations);
            newRegistrations[typeof(T).FullName!] = Registration;

            return new BackgroundConfiguration(newRegistrations);

            object Registration(IBackgroundConfiguration configuration) => serviceFactory(configuration);
        }

        public T Resolve<T>()
        {
            var requestedType = typeof(T);
            var serviceName = requestedType.FullName!;

            if (!_registrations.TryGetValue(serviceName, out var registration))
            {
                throw new InvalidOperationException($"No instance is registered for service '{serviceName}'.");
            }

            var service = registration(this);

            return service switch
            {
                null => throw new InvalidOperationException($"Null instance returned for service '{serviceName}'."),
                T typedInstance => typedInstance,
                _ => throw new InvalidOperationException($"Incorrect instance type returned for service '{serviceName}': '{registration.GetType().FullName}' instead of '{requestedType.FullName}'.")
            };

        }
    }
}