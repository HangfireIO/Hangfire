// This file is part of Hangfire.
// Copyright © 2021 Sergey Odinokov.
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
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Server;
using Microsoft.Extensions.Hosting;

namespace Hangfire
{
    public sealed class BackgroundProcessingServerHostedService : IHostedService, IDisposable
    {
        private readonly IBackgroundProcessingServer _server;

        public BackgroundProcessingServerHostedService([NotNull] IBackgroundProcessingServer server)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _server.SendStop();
            return _server.WaitForShutdownAsync(cancellationToken);
        }

        public void Dispose()
        {
            _server.Dispose();
        }
    }
}

#endif