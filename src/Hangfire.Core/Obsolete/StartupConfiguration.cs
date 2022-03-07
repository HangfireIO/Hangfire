// This file is part of Hangfire. Copyright � 2013-2014 Sergey Odinokov.
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