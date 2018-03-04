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
using Hangfire.Dashboard;

namespace Hangfire
{
    public class DashboardOptions
    {
        public DashboardOptions()
        {
            AppPath = "/";
            Authorization = new[] { new LocalRequestsOnlyAuthorizationFilter() };
            StatsPollingInterval = 2000;
            DisplayArgumentsInLists = false;
            EnableJobFilters = false;
            UseFullWidth = false;
            DefaultRecordsPerPage = 10;
        }

        /// <summary>
        /// The path for the Back To Site link. Set to <see langword="null" /> in order to hide the Back To Site link.
        /// </summary>
        public string AppPath { get; set; }

#if NETFULL
        [Obsolete("Please use `Authorization` property instead. Will be removed in 2.0.0.")]
        public IEnumerable<IAuthorizationFilter> AuthorizationFilters { get; set; }
#endif

        public IEnumerable<IDashboardAuthorizationFilter> Authorization { get; set; }

        /// <summary>
        /// The interval the /stats endpoint should be polled with.
        /// </summary>
        public int StatsPollingInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to display arguments in job lists.
        /// </summary>
        public bool DisplayArgumentsInLists { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to be able to filter the job lists.
        /// </summary>
        public bool EnableJobFilters { get; set; }


        /// <summary>
        /// Gets or sets a value indicating whether or not to use the full screen width.
        /// </summary>
        public bool UseFullWidth { get; set; }

        /// <summary>
        /// Gets or sets the default number of records per page.
        /// </summary>
        public int DefaultRecordsPerPage { get; set; }
    }
}
