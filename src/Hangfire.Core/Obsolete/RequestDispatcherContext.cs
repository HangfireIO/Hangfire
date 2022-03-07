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
using System.Text.RegularExpressions;
using Hangfire.Annotations;

// ReSharper disable once CheckNamespace
namespace Hangfire.Dashboard
{
    [Obsolete("Use the `DashboardContext` class instead. Will be removed in 2.0.0.")]
    public class RequestDispatcherContext
    {
        public RequestDispatcherContext(
            string appPath,
            int statsPollingInterval,
            [NotNull] JobStorage jobStorage,
            [NotNull] IDictionary<string, object> owinEnvironment,
            [NotNull] Match uriMatch)
        {
            if (jobStorage == null) throw new ArgumentNullException(nameof(jobStorage));
            if (owinEnvironment == null) throw new ArgumentNullException(nameof(owinEnvironment));
            if (uriMatch == null) throw new ArgumentNullException(nameof(uriMatch));

            AppPath = appPath;
            StatsPollingInterval = statsPollingInterval;
            JobStorage = jobStorage;
            OwinEnvironment = owinEnvironment;
            UriMatch = uriMatch;
        }

        public string AppPath { get; }
        public int StatsPollingInterval { get; }
        public JobStorage JobStorage { get; }
        public IDictionary<string, object> OwinEnvironment { get; } 
        public Match UriMatch { get; }

        public static RequestDispatcherContext FromDashboardContext([NotNull] DashboardContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var owinContext = context as OwinDashboardContext;
            if (owinContext == null)
            {
                throw new NotSupportedException($"context must be of type '{nameof(OwinDashboardContext)}'");
            }
            
            return new RequestDispatcherContext(
                owinContext.Options.AppPath,
                owinContext.Options.StatsPollingInterval,
                owinContext.Storage,
                owinContext.Environment,
                owinContext.UriMatch);
        }
    }
}