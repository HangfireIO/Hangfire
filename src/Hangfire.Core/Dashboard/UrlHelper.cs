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
using Hangfire.Annotations;

namespace Hangfire.Dashboard
{
    public class UrlHelper
    {
#if FEATURE_OWIN
        private readonly Microsoft.Owin.OwinContext _owinContext;

        [Obsolete("Please use UrlHelper(DashboardContext) instead. Will be removed in 2.0.0.")]
        public UrlHelper([NotNull] IDictionary<string, object> owinEnvironment)
        {
            if (owinEnvironment == null) throw new ArgumentNullException(nameof(owinEnvironment));
            _owinContext = new Microsoft.Owin.OwinContext(owinEnvironment);
        }
#endif

        private readonly DashboardContext _context;

        public UrlHelper([NotNull] DashboardContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _context = context;
        }

        public string To(string relativePath)
        {
            return _context.Options.PrefixPath +
                   (
#if FEATURE_OWIN
                       _owinContext?.Request.PathBase.Value ??
#endif
                       _context.Request.PathBase
                       + relativePath
                   );
        }

        public string Home()
        {
            return To("/");
        }

        public string JobDetails(string jobId)
        {
            return To("/jobs/details/" + jobId);
        }

        public string LinkToQueues()
        {
            return To("/jobs/enqueued");
        }

        public string Queue(string queue)
        {
            return To("/jobs/enqueued/" + queue);
        }
    }
}
