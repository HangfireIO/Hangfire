// This file is part of Hangfire. Copyright © 2016 Sergey Odinokov.
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
using Hangfire.Annotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.Dashboard
{
    public sealed class AspNetCoreDashboardContext : DashboardContext
    {
        public AspNetCoreDashboardContext(
            [NotNull] JobStorage storage,
            [NotNull] DashboardOptions options,
            [NotNull] HttpContext httpContext) 
            : base(storage, options)
        {
            HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            Request = new AspNetCoreDashboardRequest(httpContext);
            Response = new AspNetCoreDashboardResponse(httpContext);

            if (!options.IgnoreAntiforgeryToken)
            {
                var antiforgery = HttpContext.RequestServices.GetService<IAntiforgery>();
                var tokenSet = antiforgery?.GetAndStoreTokens(HttpContext);

                if (tokenSet != null)
                {
                    AntiforgeryHeader = tokenSet.HeaderName;
                    AntiforgeryToken = tokenSet.RequestToken;
                }
            }
        }

        public HttpContext HttpContext { get; }

        public override IBackgroundJobClient GetBackgroundJobClient()
        {
            var factory = HttpContext.RequestServices.GetService<IBackgroundJobClientFactory>();
            if (factory != null)
            {
                return factory.GetClient(Storage);
            }

            return HttpContext.RequestServices.GetService<IBackgroundJobClient>() ?? base.GetBackgroundJobClient();
        }

        public override IRecurringJobManager GetRecurringJobManager()
        {
            var factory = HttpContext.RequestServices.GetService<IRecurringJobManagerFactory>();
            if (factory != null)
            {
                return factory.GetManager(Storage);
            }

            return HttpContext.RequestServices.GetService<IRecurringJobManager>() ?? base.GetRecurringJobManager();
        }
    }
}
