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

using System;
using System.Collections.Generic;
using HangFire.Dashboard.Authorization;

namespace HangFire
{
    internal class BootstrapperConfiguration : IBootstrapperConfiguration
    {
        public BootstrapperConfiguration()
        {
            Servers = new List<Func<BackgroundJobServer>>();
            DashboardPath = "/hangfire";

            AuthorizationFilters = new IAuthorizationFilter[]
            {
                new LocalRequestsOnlyAuthorizationFilter()
            };

            Filters = new List<object>();
        }

        public string DashboardPath { get; private set; }
        public JobStorage Storage { get; private set; }
        public JobActivator Activator { get; private set; }
        public List<Func<BackgroundJobServer>> Servers { get; private set; }
        public IAuthorizationFilter[] AuthorizationFilters { get; private set; }
        public List<object> Filters { get; private set; } 

        public void UseAuthorizationFilters(params IAuthorizationFilter[] filters)
        {
            AuthorizationFilters = filters;
        }

        public void UseFilter(object filter)
        {
            Filters.Add(filter);
        }

        public void UseDashboardPath(string path)
        {
            DashboardPath = path;
        }

        public void UseStorage(JobStorage storage)
        {
            Storage = storage;
        }

        public void UseActivator(JobActivator activator)
        {
            Activator = activator;
        }

        public void UseServer(Func<BackgroundJobServer> server)
        {
            Servers.Add(server);
        }
    }
}