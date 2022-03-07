// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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
