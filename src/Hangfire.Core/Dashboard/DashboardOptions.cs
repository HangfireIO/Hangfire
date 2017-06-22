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

namespace Hangfire.Dashboard
{
    public class DashboardOptions
    {
        public DashboardOptions()
        {
            AppPath = "/";
            StatsPollingInterval = 2000;
            Authorizations = new Dictionary<DashboardPermission, IEnumerable<IDashboardAuthorizationFilter>>
            {
                [DashboardPermission.ViewDashboard] = new[] { new LocalRequestsOnlyAuthorizationFilter() },
                [DashboardPermission.EnqueueJob] = new IDashboardAuthorizationFilter[0],
                [DashboardPermission.DeleteJob] = new IDashboardAuthorizationFilter[0]
            };
        }

        /// <summary>
        /// The path for the Back To Site link. Set to <see langword="null" /> in order to hide the Back To Site link.
        /// </summary>
        public string AppPath { get; set; }

#if NETFULL
        [Obsolete("Please use `ViewDashboardAuthorization` property instead. Will be removed in 2.0.0.")]
        public IEnumerable<IAuthorizationFilter> AuthorizationFilters { get; set; }
#endif

        [Obsolete("Please use `ViewDashboardAuthorization` property instead.")]
        public IEnumerable<IDashboardAuthorizationFilter> Authorization
        {
            get { return ViewDashboardAuthorization; }
            set { ViewDashboardAuthorization = value; }
        }

        public IEnumerable<IDashboardAuthorizationFilter> ViewDashboardAuthorization
        {
            get { return Authorizations[DashboardPermission.ViewDashboard]; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                Authorizations[DashboardPermission.ViewDashboard] = value;
            }
        }

        public IEnumerable<IDashboardAuthorizationFilter> EnqueueJobAuthorization
        {
            get { return Authorizations[DashboardPermission.EnqueueJob]; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                Authorizations[DashboardPermission.EnqueueJob] = value;
            }
        }

        public IEnumerable<IDashboardAuthorizationFilter> DeleteJobAuthorization
        { 
            get { return Authorizations[DashboardPermission.DeleteJob]; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                Authorizations[DashboardPermission.DeleteJob] = value;
            }
        }

        internal IDictionary<DashboardPermission, IEnumerable<IDashboardAuthorizationFilter>> Authorizations { get; }

        /// <summary>
        /// The interval the /stats endpoint should be polled with.
        /// </summary>
        public int StatsPollingInterval { get; set; }
    }
}
