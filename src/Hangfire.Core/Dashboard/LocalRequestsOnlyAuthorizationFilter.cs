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

#if NETFULL
using Microsoft.Owin;
#else
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
#endif

namespace Hangfire.Dashboard
{
    public class LocalRequestsOnlyAuthorizationFilter : IAuthorizationFilter
    {
        public bool Authorize(
#if NETFULL
            IDictionary<string, object> owinEnvironment
#else
            HttpContext context
#endif
            )
        {
#if NETFULL
            var context = new OwinContext(owinEnvironment);
#else
            var connection = context.Features.Get<IHttpConnectionFeature>();
#endif
            var remoteAddress =
#if NETFULL
                context.Request.RemoteIpAddress
#else
                connection.RemoteIpAddress.ToString()
#endif
                ;

            // if unknown, assume not local
            if (String.IsNullOrEmpty(remoteAddress))
                return false;

            // check if localhost
            if (remoteAddress == "127.0.0.1" || remoteAddress == "::1")
                return true;

            // compare with local address
            if (remoteAddress ==
#if NETFULL
                context.Request.LocalIpAddress
#else
                connection.LocalIpAddress.ToString()
#endif
                )
                return true;

            return false;
        }
    }
}
