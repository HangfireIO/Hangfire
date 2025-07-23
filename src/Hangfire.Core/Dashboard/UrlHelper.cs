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

// ReSharper disable RedundantNullnessAttributeWithNullableReferenceTypes
#nullable enable

namespace Hangfire.Dashboard
{
    public class UrlHelper
    {
#if FEATURE_OWIN
        private readonly Microsoft.Owin.OwinContext? _owinContext;

        [Obsolete("Please use UrlHelper(DashboardContext) instead. Will be removed in 2.0.0.")]
        public UrlHelper([NotNull] IDictionary<string, object> owinEnvironment)
        {
            if (owinEnvironment == null) throw new ArgumentNullException(nameof(owinEnvironment));
            _owinContext = new Microsoft.Owin.OwinContext(owinEnvironment);
        }
#endif

        private readonly DashboardContext? _context;

        public UrlHelper([NotNull] DashboardContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [NotNull]
        public string To([NotNull] string relativePath)
        {
            if (relativePath == null) throw new ArgumentNullException(nameof(relativePath));
            
#if FEATURE_OWIN
            if (_owinContext != null)
            {
                return _owinContext.Request.PathBase.Value + relativePath;
            }
#endif
            return _context!.Options.PrefixPath + _context.Request.PathBase + relativePath;
        }

        [NotNull]
        public string Home()
        {
            return To("/");
        }

        [NotNull]
        public string JobDetails([NotNull] string jobId)
        {
            if (jobId == null) throw new ArgumentNullException(nameof(jobId));
            return To("/jobs/details/" + jobId);
        }

        [NotNull]
        public string LinkToQueues()
        {
            return To("/jobs/enqueued");
        }

        [NotNull]
        public string Queue([NotNull] string queue)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));
            return To("/jobs/enqueued/" + queue);
        }
    }
}
