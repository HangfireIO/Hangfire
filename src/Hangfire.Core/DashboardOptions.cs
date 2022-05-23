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
using Hangfire.Common;
using Hangfire.Dashboard;

namespace Hangfire
{
    public class DashboardOptions
    {
        private static readonly IDashboardAuthorizationFilter[] DefaultAuthorization =
            new[] { new LocalRequestsOnlyAuthorizationFilter() };

        private IEnumerable<IDashboardAsyncAuthorizationFilter> _asyncAuthorization;

        public DashboardOptions()
        {
            AppPath = "/";
            PrefixPath = string.Empty;
            _asyncAuthorization = new IDashboardAsyncAuthorizationFilter[0];
            Authorization = DefaultAuthorization;
            IsReadOnlyFunc = _ => false;
            StatsPollingInterval = 2000;
            DisplayStorageConnectionString = true;
            DashboardTitle = "Hangfire Dashboard";
            DisplayNameFunc = null;
        }

        /// <summary>
        /// The path for the Back To Site link. Set to <see langword="null" /> in order to hide the Back To Site link.
        /// </summary>
        public string AppPath { get; set; }
        
        /// <summary>
        /// The path for the first url prefix link, eg. set "/admin", then url is "{domain}/{PrefixPath}/{hangfire}"
        /// </summary>
        public string PrefixPath { get; set; }


#if FEATURE_OWIN
        [Obsolete("Please use `Authorization` property instead. Will be removed in 2.0.0.")]
        public IEnumerable<IAuthorizationFilter> AuthorizationFilters { get; set; }
#endif

        public IEnumerable<IDashboardAuthorizationFilter> Authorization { get; set; }

        public IEnumerable<IDashboardAsyncAuthorizationFilter> AsyncAuthorization
        {
            get => _asyncAuthorization;
            set
            {
                _asyncAuthorization = value;

                if (ReferenceEquals(Authorization, DefaultAuthorization))
                {
                    Authorization = new IDashboardAuthorizationFilter[0];
                }
            }
        }

        public Func<DashboardContext, bool> IsReadOnlyFunc { get; set; }
        
        /// <summary>
        /// The interval the /stats endpoint should be polled with.
        /// </summary>
        public int StatsPollingInterval { get; set; }

        public bool DisplayStorageConnectionString { get; set; }

        /// <summary>
        /// The Title displayed on the dashboard, optionally modify to describe this dashboards purpose.
        /// </summary>
        public string DashboardTitle { get; set; }

        /// <summary>
        /// Display name provider for jobs
        /// </summary>
        public Func<DashboardContext, Job, string> DisplayNameFunc { get; set; }

        public bool IgnoreAntiforgeryToken { get; set; }

        public ITimeZoneResolver TimeZoneResolver { get; set; }
    }
}
