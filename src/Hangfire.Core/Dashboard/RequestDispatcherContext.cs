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
using System.Text.RegularExpressions;
using Hangfire.Annotations;

namespace Hangfire.Dashboard
{
    public class RequestDispatcherContext
    {
        public RequestDispatcherContext(
            [NotNull] string backToSitePath,
            [NotNull] JobStorage jobStorage,
            [NotNull] IDictionary<string, object> owinEnvironment, 
            [NotNull] Match uriMatch)
        {
            if (backToSitePath == null) throw new ArgumentNullException("backToSitePath");
            if (jobStorage == null) throw new ArgumentNullException("jobStorage");
            if (owinEnvironment == null) throw new ArgumentNullException("owinEnvironment");
            if (uriMatch == null) throw new ArgumentNullException("uriMatch");

            BackToSitePath = backToSitePath;
            JobStorage = jobStorage;
            OwinEnvironment = owinEnvironment;
            UriMatch = uriMatch;
        }

        public string BackToSitePath { get; private set; }
        public JobStorage JobStorage { get; private set; }
        public IDictionary<string, object> OwinEnvironment { get; private set; } 
        public Match UriMatch { get; private set; }
    }
}