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
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Dashboard.Owin;
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
            [NotNull] RouteCollection routes,
            [CanBeNull] IOwinDashboardAntiforgery antiforgery)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (routes == null) throw new ArgumentNullException(nameof(routes));

            builder(_ => UseHangfireDashboard(options, storage, routes, antiforgery));

            return builder;
        }

        public static MidFunc UseHangfireDashboard(
            [NotNull] DashboardOptions options, 
            [NotNull] JobStorage storage, 
            [NotNull] RouteCollection routes,
            [CanBeNull] IOwinDashboardAntiforgery antiforgery)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (routes == null) throw new ArgumentNullException(nameof(routes));

            return
                next =>
                async env =>
                {
                    var owinContext = new OwinContext(env);
                    var context = new OwinDashboardContext(storage, options, env);

                    if (!options.IgnoreAntiforgeryToken && antiforgery != null)
                    {
                        context.AntiforgeryHeader = antiforgery.HeaderName;
                        context.AntiforgeryToken = antiforgery.GetToken(env);
                    }

#pragma warning disable 618
                    if (options.AuthorizationFilters != null)
                    {
                        if (options.AuthorizationFilters.Any(filter => !filter.Authorize(owinContext.Environment)))
#pragma warning restore 618
                        {
                            owinContext.Response.StatusCode = GetUnauthorizedStatusCode(owinContext);
                            return;
                        }
                    }
                    else
                    {
                        // ReSharper disable once LoopCanBeConvertedToQuery
                        foreach (var filter in options.Authorization)
                        {
                            if (!filter.Authorize(context))
                            {
                                owinContext.Response.StatusCode = GetUnauthorizedStatusCode(owinContext);
                                return;
                            }
                        }

                        foreach (var filter in options.AsyncAuthorization)
                        {
                            if (!await filter.AuthorizeAsync(context))
                            {
                                owinContext.Response.StatusCode = GetUnauthorizedStatusCode(owinContext);
                                return;
                            }
                        }
                    }

                    if (!options.IgnoreAntiforgeryToken && antiforgery != null && !antiforgery.ValidateRequest(env))
                    {
                        owinContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        return;
                    }

                    var findResult = routes.FindDispatcher(owinContext.Request.Path.Value);

                    if (findResult == null)
                    {
                        await next(env);
                        return;
                    }

                    context.UriMatch = findResult.Item2;

                    await findResult.Item1.Dispatch(context);
                };
        }

        private static int GetUnauthorizedStatusCode(IOwinContext owinContext)
        {
            return owinContext.Authentication?.User?.Identity?.IsAuthenticated == true
                ? (int)HttpStatusCode.Forbidden
                : (int)HttpStatusCode.Unauthorized;
        }
    }
}