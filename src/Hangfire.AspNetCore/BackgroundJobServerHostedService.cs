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

#if NETCOREAPP3_0 || NETSTANDARD2_0 || NET461
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.Hosting;

namespace Hangfire
{
    public class BackgroundJobServerHostedService : IHostedService, IDisposable
    {
        private readonly BackgroundJobServerOptions _options;
        private readonly JobStorage _storage;
        private readonly IEnumerable<IBackgroundProcess> _additionalProcesses;
#if NETCOREAPP3_0_OR_GREATER
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
#endif
        private readonly IBackgroundJobFactory _factory;
        private readonly IBackgroundJobPerformer _performer;
        private readonly IBackgroundJobStateChanger _stateChanger;

        private IBackgroundProcessingServer _processingServer;

        public BackgroundJobServerHostedService(
            [NotNull] JobStorage storage,
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses)
#pragma warning disable 618
            : this(storage, options, additionalProcesses, null, null, null)
#pragma warning restore 618
        {
        }

#if NETCOREAPP3_0_OR_GREATER
        public BackgroundJobServerHostedService(
            [NotNull] JobStorage storage,
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses,
            [CanBeNull] IHostApplicationLifetime hostApplicationLifetime)
#pragma warning disable 618
            : this(storage, options, additionalProcesses, null, null, null, hostApplicationLifetime)
#pragma warning restore 618
        {
        }
#endif

#if NETCOREAPP3_0_OR_GREATER
        [Obsolete("This constructor uses an obsolete constructor overload of the BackgroundJobServer type that will be removed in 2.0.0.")]
        public BackgroundJobServerHostedService(
            [NotNull] JobStorage storage,
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses,
            [CanBeNull] IBackgroundJobFactory factory,
            [CanBeNull] IBackgroundJobPerformer performer,
            [CanBeNull] IBackgroundJobStateChanger stateChanger)
            : this(storage, options, additionalProcesses, factory, performer, stateChanger, null)
        {
        }
#endif

        [Obsolete("This constructor uses an obsolete constructor overload of the BackgroundJobServer type that will be removed in 2.0.0.")]
        public BackgroundJobServerHostedService(
            [NotNull] JobStorage storage,
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses,
            [CanBeNull] IBackgroundJobFactory factory,
            [CanBeNull] IBackgroundJobPerformer performer,
            [CanBeNull] IBackgroundJobStateChanger stateChanger
#if NETCOREAPP3_0_OR_GREATER
            ,
            [CanBeNull] IHostApplicationLifetime hostApplicationLifetime
#endif
            )
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            _additionalProcesses = additionalProcesses;

            _factory = factory;
            _performer = performer;
            _stateChanger = stateChanger;

#if NETCOREAPP3_0_OR_GREATER
            _hostApplicationLifetime = hostApplicationLifetime;
#endif
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
#if NETCOREAPP3_0_OR_GREATER
            if (_hostApplicationLifetime != null)
            {
                // https://github.com/HangfireIO/Hangfire/issues/2117
                _hostApplicationLifetime.ApplicationStarted.Register(InitializeProcessingServer);
            }
            else
#endif
            {
                InitializeProcessingServer();                
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _processingServer?.SendStop();
            return _processingServer?.WaitForShutdownAsync(cancellationToken) ?? Task.CompletedTask;
        }

        public void Dispose()
        {
            _processingServer?.Dispose();
            _processingServer = null;
        }
        
        private void InitializeProcessingServer()
        {
            _processingServer = _factory != null && _performer != null && _stateChanger != null
#pragma warning disable 618
                ? new BackgroundJobServer(_options, _storage, _additionalProcesses, null, null, _factory, _performer,
                    _stateChanger)
#pragma warning restore 618
                : new BackgroundJobServer(_options, _storage, _additionalProcesses);
        }
    }
}
#endif
