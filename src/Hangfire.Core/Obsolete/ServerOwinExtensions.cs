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
using System.Threading;
using Microsoft.Owin;
using Owin;

// ReSharper disable once CheckNamespace
namespace Hangfire.Server
{
    /// <exclude />
    [Obsolete("Please use `IAppBuilder.UseHangfireServer` OWIN extension method instead. Will be removed in version 2.0.0.")]
    public static class ServerOwinExtensions
    {
        // Prevent GC to collect background servers in hosts that do not
        // support shutdown notifications.
        private static readonly ConcurrentBag<BackgroundJobServer> Servers 
            = new ConcurrentBag<BackgroundJobServer>(); 

        /// <summary>
        /// Starts the specified background job server and registers the call
        /// to its `Dispose` method at OWIN application's shutdown event.
        /// </summary>
        /// <param name="app">The app builder</param>
        /// <param name="server">The background job server to start</param>
        [Obsolete("Please use `IAppBuilder.UseHangfireServer` OWIN extension method instead. Will be removed in version 2.0.0.")]
        public static void RunHangfireServer(
            this IAppBuilder app,
            BackgroundJobServer server)
        {
            Servers.Add(server);

            server.Start();

            var context = new OwinContext(app.Properties);
            var token = context.Get<CancellationToken>("host.OnAppDisposing");

            if (token != CancellationToken.None)
            {
                token.Register(server.Dispose);
            }
        }
    }
}
