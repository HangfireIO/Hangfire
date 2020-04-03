// This file is part of Hangfire.
// Copyright � 2016 Sergey Odinokov.
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
using System.Net;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.Dashboard
{
    public class AspNetCoreDashboardMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly JobStorage _storage;
        private readonly IClock _clock;
        private readonly DashboardOptions _options;
        private readonly RouteCollection _routes;

        public AspNetCoreDashboardMiddleware(
            [NotNull] RequestDelegate next,
            [NotNull] JobStorage storage,
            [NotNull] IClock clock,
            [NotNull] DashboardOptions options,
            [NotNull] RouteCollection routes)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (routes == null) throw new ArgumentNullException(nameof(routes));

            _next = next;
            _storage = storage;
            _clock = clock;
            _options = options;
            _routes = routes;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var context = new AspNetCoreDashboardContext(_storage, _clock, _options, httpContext);
            var findResult = _routes.FindDispatcher(httpContext.Request.Path.Value);

            if (findResult == null)
            {
                await _next.Invoke(httpContext);
                return;
            }

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var filter in _options.Authorization)
            {
                if (!filter.Authorize(context))
                {
                    var isAuthenticated = httpContext.User?.Identity?.IsAuthenticated;

                    httpContext.Response.StatusCode = isAuthenticated == true
                        ? (int) HttpStatusCode.Forbidden
                        : (int) HttpStatusCode.Unauthorized;

                    return;
                }
            }

            if (!_options.IgnoreAntiforgeryToken)
            {
                var antiforgery = httpContext.RequestServices.GetService<IAntiforgery>();

                if (antiforgery != null)
                {
                    var requestValid = await antiforgery.IsRequestValidAsync(httpContext);

                    if (!requestValid)
                    {
                        // Invalid or missing CSRF token
                        httpContext.Response.StatusCode = (int) HttpStatusCode.Forbidden;
                        return;
                    }
                }
            }

            context.UriMatch = findResult.Item2;

            await findResult.Item1.Dispatch(context);
        }
    }
}
