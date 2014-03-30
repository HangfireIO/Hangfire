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

using System.Collections.Generic;
using HangFire.Server.Performing;

namespace HangFire.Server
{
    public class ServerContext
    {
        internal ServerContext(ServerContext context)
            : this(context.ServerName, context.QueueNames, context.PerformancePipeline)
        {
        }

        internal ServerContext(
            string serverName,
            IEnumerable<string> queueNames,
            JobPerformancePipeline performancePipeline)
        {
            ServerName = serverName;
            PerformancePipeline = performancePipeline;
            QueueNames = queueNames;
        }

        public string ServerName { get; private set; }
        public IEnumerable<string> QueueNames { get; private set; }

        internal JobPerformancePipeline PerformancePipeline { get; private set; }
    }
}
