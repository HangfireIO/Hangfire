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
