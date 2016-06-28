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
using Hangfire.Annotations;
#if NETFULL
using Microsoft.Owin;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace Hangfire.Dashboard
{
    public class UrlHelper
    {
#if NETFULL
        private readonly OwinContext _context;
#else
        private readonly HttpRequest _request;
#endif

        public UrlHelper(
#if NETFULL
            [NotNull] IDictionary<string, object> owinContext
#else
            [NotNull] HttpRequest request
#endif
            )
        {
#if NETFULL
            if (owinContext == null) throw new ArgumentNullException(nameof(owinContext));
            _context = new OwinContext(owinContext);
#else
            if (request == null) throw new ArgumentNullException(nameof(request));
            _request = request;
#endif
        }

        public string To(string relativePath)
        {
            return
#if NETFULL
                _context.Request.PathBase 
#else
                _request.PathBase
#endif
                + relativePath;
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
