// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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
