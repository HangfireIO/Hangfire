using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Dashboard;

namespace Hangfire.Core.Tests.Dashboard
{
    internal class UnauthorizedDashboardFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return false;
        }
    }
}
