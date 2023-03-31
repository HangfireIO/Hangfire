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
using System.Collections.Generic;
using Hangfire.Dashboard;

namespace Hangfire
{
    /// <exclude />
    [Obsolete]
    internal class BootstrapperConfiguration : IBootstrapperConfiguration
    {
        public BootstrapperConfiguration()
        {
            Servers = new List<Func<BackgroundJobServer>>();
            DashboardPath = DashboardOwinExtensions.DefaultDashboardPath;
            AppPath = DashboardOwinExtensions.DefaultAppPath;

            AuthorizationFilters = DashboardOwinExtensions.DefaultAuthorizationFilters;

            Filters = new List<object>();
        }

        public string DashboardPath { get; private set; }
        public string AppPath { get; private set; }
        public JobStorage Storage { get; private set; }
        public JobActivator Activator { get; private set; }
        public List<Func<BackgroundJobServer>> Servers { get; }
        public IAuthorizationFilter[] AuthorizationFilters { get; private set; }
        public List<object> Filters { get; } 

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

        public void UseAppPath(string path)
        {
            AppPath = path;
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