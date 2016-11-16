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
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Microsoft.Owin;

namespace Hangfire.Dashboard
{
    using MidFunc = Func<
        Func<IDictionary<string, object>, Task>,
        Func<IDictionary<string, object>, Task>
        >;
    using BuildFunc = Action<
        Func<
            IDictionary<string, object>,
            Func<
                Func<IDictionary<string, object>, Task>,
                Func<IDictionary<string, object>, Task>
        >>>;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MiddlewareExtensions
    {
        public static BuildFunc UseHangfireDashboard(
            [NotNull] this BuildFunc builder,
            [NotNull] DashboardOptions options, 
            [NotNull] JobStorage storage, 
            [NotNull] RouteCollection routes)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (routes == null) throw new ArgumentNullException(nameof(routes));

            builder(_ => UseHangfireDashboard(options, storage, routes));

            return builder;
        }

        public static MidFunc UseHangfireDashboard(
            [NotNull] DashboardOptions options, 
            [NotNull] JobStorage storage, 
            [NotNull] RouteCollection routes)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (routes == null) throw new ArgumentNullException(nameof(routes));

            return
                next =>
                env =>
                {
                    var owinContext = new OwinContext(env);
                    var context = new OwinDashboardContext(storage, options, env);

#pragma warning disable 618
                    if (options.AuthorizationFilters != null)
                    {
                        if (options.AuthorizationFilters.Any(filter => !filter.Authorize(owinContext.Environment)))
#pragma warning restore 618
                        {
                            return Unauthorized(owinContext);
                        }
                    }
                    else
                    {
                        if (options.Authorization.Any(filter => !filter.Authorize(context)))
                        {
                            return Unauthorized(owinContext);
                        }
                    }

                    var findResult = routes.FindDispatcher(owinContext.Request.Path.Value);

                    if (findResult == null)
                    {
                        return next(env);
                    }

                    context.UriMatch = findResult.Item2;

                    return findResult.Item1.Dispatch(context);
                };
        }

        private static Task Unauthorized(IOwinContext owinContext)
        {
            owinContext.Response.StatusCode = owinContext.Authentication.User.Identity.IsAuthenticated
                ? (int)HttpStatusCode.Forbidden
                : (int)HttpStatusCode.Unauthorized;

            return Task.FromResult(0);
        }
    }
}