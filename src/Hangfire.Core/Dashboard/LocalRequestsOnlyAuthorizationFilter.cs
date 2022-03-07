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

namespace Hangfire.Dashboard
{
    public class LocalRequestsOnlyAuthorizationFilter : IDashboardAuthorizationFilter
#if FEATURE_OWIN
#pragma warning disable 618
        , IAuthorizationFilter
#pragma warning restore 618
#endif
    {
        public bool Authorize(DashboardContext context)
        {
            // if unknown, assume not local
            if (String.IsNullOrEmpty(context.Request.RemoteIpAddress))
                return false;

            // check if localhost
            if (context.Request.RemoteIpAddress == "127.0.0.1" || context.Request.RemoteIpAddress == "::1")
                return true;

            // compare with local address
            if (context.Request.RemoteIpAddress == context.Request.LocalIpAddress)
                return true;

            return false;
        }

#if FEATURE_OWIN
        public bool Authorize(IDictionary<string, object> owinEnvironment)
        {
            var context = new Microsoft.Owin.OwinContext(owinEnvironment);

            // if unknown, assume not local
            if (String.IsNullOrEmpty(context.Request.RemoteIpAddress))
                return false;

            // check if localhost
            if (context.Request.RemoteIpAddress == "127.0.0.1" || context.Request.RemoteIpAddress == "::1")
                return true;

            // compare with local address
            if (context.Request.RemoteIpAddress == context.Request.LocalIpAddress)
                return true;

            return false;
        }
#endif
    }
}
