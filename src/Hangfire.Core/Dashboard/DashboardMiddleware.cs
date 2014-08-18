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
using System.Net;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Microsoft.Owin;

namespace Hangfire.Dashboard
{
    public class DashboardMiddleware : OwinMiddleware
    {
        private readonly JobStorage _storage;
        private readonly RouteCollection _routes;
        private readonly IEnumerable<IAuthorizationFilter> _authorizationFilters;

        public DashboardMiddleware(
            OwinMiddleware next, 
            [NotNull] JobStorage storage,
            [NotNull] RouteCollection routes, 
            [NotNull] IEnumerable<IAuthorizationFilter> authorizationFilters)
            : base(next)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (routes == null) throw new ArgumentNullException("routes");
            if (authorizationFilters == null) throw new ArgumentNullException("authorizationFilters");

            _storage = storage;
            _routes = routes;
            _authorizationFilters = authorizationFilters;
        }

        public override Task Invoke(IOwinContext context)
        {
            var dispatcher = _routes.FindDispatcher(context.Request.Path.Value);
            
            if (dispatcher == null)
            {
                return Next.Invoke(context);
            }

            foreach (var filter in _authorizationFilters)
            {
                if (!filter.Authorize(context))
                {
                    context.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    return Task.FromResult(false);
                }
            }

            var dispatcherContext = new RequestDispatcherContext(
                _storage,
                context,
                dispatcher.Item2);

            return dispatcher.Item1.Dispatch(dispatcherContext);
        }
    }
}
