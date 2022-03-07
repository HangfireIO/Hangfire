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
using System.Net;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Microsoft.Owin;

#pragma warning disable 618

// ReSharper disable once CheckNamespace
namespace Hangfire.Dashboard
{
    internal class DashboardMiddleware : OwinMiddleware
    {
        private readonly string _appPath;
        private readonly int _statsPollingInterval;
        private readonly JobStorage _storage;
        private readonly RouteCollection _routes;
        private readonly IEnumerable<IAuthorizationFilter> _authorizationFilters;

        public DashboardMiddleware(
            OwinMiddleware next,
            string appPath,
            int statsPollingInterval,
            [NotNull] JobStorage storage,
            [NotNull] RouteCollection routes, 
            [NotNull] IEnumerable<IAuthorizationFilter> authorizationFilters)
            : base(next)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            if (authorizationFilters == null) throw new ArgumentNullException(nameof(authorizationFilters));

            _appPath = appPath;
            _statsPollingInterval = statsPollingInterval;
            _storage = storage;
            _routes = routes;
            _authorizationFilters = authorizationFilters;
        }

        public override Task Invoke(IOwinContext owinContext)
        {
            var dispatcher = _routes.FindDispatcher(owinContext.Request.Path.Value);
            
            if (dispatcher == null)
            {
                return Next.Invoke(owinContext);
            }

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var filter in _authorizationFilters)
            {
                if (!filter.Authorize(owinContext.Environment))
                {
                    owinContext.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    return owinContext.Response.WriteAsync("401 Unauthorized");
                }
            }
            
            var context = new OwinDashboardContext(
                _storage,
                new DashboardOptions { AppPath = _appPath, StatsPollingInterval = _statsPollingInterval, AuthorizationFilters = _authorizationFilters }, 
                owinContext.Environment);

            return dispatcher.Item1.Dispatch(context);
        }
    }
}
