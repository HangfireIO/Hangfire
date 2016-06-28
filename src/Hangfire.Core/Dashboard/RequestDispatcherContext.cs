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
using System.Text.RegularExpressions;
using Hangfire.Annotations;

#if NETFULL
using System.Collections.Generic;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace Hangfire.Dashboard
{
    public class RequestDispatcherContext
    {
        public RequestDispatcherContext(
            string appPath,
            int statsPollingInterval,
            [NotNull] JobStorage jobStorage,
#if NETFULL
            [NotNull] IDictionary<string, object> owinEnvironment,
#else
            [NotNull] HttpContext http,
#endif
            [NotNull] Match uriMatch)
        {
            if (jobStorage == null) throw new ArgumentNullException(nameof(jobStorage));
#if NETFULL
            if (owinEnvironment == null) throw new ArgumentNullException(nameof(owinEnvironment));
#else
            if (http == null) throw new ArgumentNullException(nameof(http));
#endif
            if (uriMatch == null) throw new ArgumentNullException(nameof(uriMatch));

            AppPath = appPath;
            StatsPollingInterval = statsPollingInterval;
            JobStorage = jobStorage;
#if NETFULL
            OwinEnvironment = owinEnvironment;
#else
            Http = http;
#endif
            UriMatch = uriMatch;
        }

        public string AppPath { get; }
        public int StatsPollingInterval { get; }
        public JobStorage JobStorage { get; }
#if NETFULL
        public IDictionary<string, object> OwinEnvironment { get; } 
#else
        public HttpContext Http { get; }
#endif
        public Match UriMatch { get; }
    }
}