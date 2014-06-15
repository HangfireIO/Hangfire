// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Owin;
using Owin;

namespace HangFire.Server
{
    public static class ServerOwinExtensions
    {
        // Prevent GC to collect background servers in hosts that do not
        // support shutdown notifications.
        private static readonly ConcurrentBag<BackgroundJobServer> Servers 
            = new ConcurrentBag<BackgroundJobServer>(); 

        public static void RunHangFireServer(
            this IAppBuilder app,
            BackgroundJobServer server)
        {
            Servers.Add(server);

            server.Start();

            var context = new OwinContext(app.Properties);
            var token = context.Get<CancellationToken>("host.OnAppDisposing");

            if (token != CancellationToken.None)
            {
                token.Register(server.Stop);
            }
        }
    }
}
