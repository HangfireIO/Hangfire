﻿// This file is part of Hangfire.
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
using FiltersDictionary = System.Collections.Generic.IDictionary<Hangfire.Dashboard.DashboardPermission, System.Collections.Generic.IEnumerable<Hangfire.Dashboard.IDashboardAuthorizationFilter>>;

namespace Hangfire.Dashboard
{
    public class DashboardOptions
    {
        IDictionary<DashboardPermission, IEnumerable<IDashboardAuthorizationFilter>> _authorizations;

        public DashboardOptions()
        {
            AppPath = "/";
            Authorization = new[] { new LocalRequestsOnlyAuthorizationFilter() };
            StatsPollingInterval = 2000;
        }

        /// <summary>
        /// The path for the Back To Site link. Set to <see langword="null" /> in order to hide the Back To Site link.
        /// </summary>
        public string AppPath { get; set; }

#if NETFULL
        [Obsolete("Please use `Authorizations` property instead. Will be removed in 2.0.0.")]
        public IEnumerable<IAuthorizationFilter> AuthorizationFilters { get; set; }
#endif

        [Obsolete("Please use `Authorizations` property instead.")]
        public IEnumerable<IDashboardAuthorizationFilter> Authorization
        {
            get { return Authorizations[DashboardPermission.ViewDashboard]; }
            set { Authorizations[DashboardPermission.ViewDashboard] = value; }
        }

        public IDictionary<DashboardPermission, IEnumerable<IDashboardAuthorizationFilter>> Authorizations
        {
            get { return _authorizations; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _authorizations = value;
                if (!_authorizations.ContainsKey(DashboardPermission.ViewDashboard))
                {
                    _authorizations[DashboardPermission.ViewDashboard] = new[] { new LocalRequestsOnlyAuthorizationFilter() };
                }
            }
        }

        /// <summary>
        /// The interval the /stats endpoint should be polled with.
        /// </summary>
        public int StatsPollingInterval { get; set; }
    }
}
