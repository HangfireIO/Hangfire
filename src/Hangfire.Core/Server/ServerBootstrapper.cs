// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.Server
{
    internal class ServerBootstrapper : IBackgroundProcess
    {
        private readonly IEnumerable<IServerProcess> _processes;

        public ServerBootstrapper(IEnumerable<IServerProcess> processes)
        {
            if (processes == null) throw new ArgumentNullException("processes");
            _processes = processes;
        }

        public void Execute(BackgroundProcessContext context)
        {
            using (var connection = context.Storage.GetConnection())
            {
                var serverContext = new ServerContext();

                if (context.Properties.ContainsKey("Queues"))
                {
                    var array = context.Properties["Queues"] as string[];
                    if (array != null) { serverContext.Queues = array; }
                }

                if (context.Properties.ContainsKey("WorkerCount"))
                {
                    serverContext.WorkerCount = (int)context.Properties["WorkerCount"];
                }

                connection.AnnounceServer(context.ServerId, serverContext);
            }

            try
            {
                var tasks = _processes
                    .Select(process => process.CreateTask(context))
                    .ToArray();

                Task.WaitAll(tasks);
            }
            finally
            {
                using (var connection = context.Storage.GetConnection())
                {
                    connection.RemoveServer(context.ServerId);
                }
            }
        }

        public override string ToString()
        {
            return "Server Bootstrapper";
        }
    }
}
