﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Dashboard;

namespace Hangfire.Core.Tests.Dashboard
{
    internal class AuthorizedDashboardFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(IDashboardContext context)
        {
            return true;
        }
    }
}
