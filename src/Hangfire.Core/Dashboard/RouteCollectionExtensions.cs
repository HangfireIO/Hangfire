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
using System.Text.RegularExpressions;
using Hangfire.Annotations;
using System.ComponentModel;

namespace Hangfire.Dashboard
{
    public static class RouteCollectionExtensions
    {
        public static void AddRazorPage(
            [NotNull] this RouteCollection routes, 
            [NotNull] string pathTemplate, 
            [NotNull] Func<Match, RazorPage> pageFunc)
        {
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null) throw new ArgumentNullException(nameof(pathTemplate));
            if (pageFunc == null) throw new ArgumentNullException(nameof(pageFunc));

            routes.Add(pathTemplate, new RazorPageDispatcher(pageFunc));
        }

#if FEATURE_OWIN
        [Obsolete("Use the AddCommand(RouteCollection, string, Func<DashboardContext, bool>) overload instead. Will be removed in 2.0.0.")]
        public static void AddCommand(
            [NotNull] this RouteCollection routes, 
            [NotNull] string pathTemplate, 
            [NotNull] Func<RequestDispatcherContext, bool> command)
        {
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null) throw new ArgumentNullException(nameof(pathTemplate));
            if (command == null) throw new ArgumentNullException(nameof(command));

            routes.Add(pathTemplate, new CommandDispatcher(command));
        }
#endif

        public static void AddCommand(
            [NotNull] this RouteCollection routes,
            [NotNull] string pathTemplate,
            [NotNull] Func<DashboardContext, bool> command)
        {
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null) throw new ArgumentNullException(nameof(pathTemplate));
            if (command == null) throw new ArgumentNullException(nameof(command));

            routes.Add(pathTemplate, new CommandDispatcher(command));
        }

#if FEATURE_OWIN
        [Obsolete("Use the AddBatchCommand(RouteCollection, string, Func<DashboardContext, bool>) overload instead. Will be removed in 2.0.0.")]
        public static void AddBatchCommand(
            [NotNull] this RouteCollection routes, 
            [NotNull] string pathTemplate, 
            [NotNull] Action<RequestDispatcherContext, string> command)
        {
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null) throw new ArgumentNullException(nameof(pathTemplate));
            if (command == null) throw new ArgumentNullException(nameof(command));

            routes.Add(pathTemplate, new BatchCommandDispatcher(command));
        }
#endif

        public static void AddBatchCommand(
            [NotNull] this RouteCollection routes,
            [NotNull] string pathTemplate,
            [NotNull] Action<DashboardContext, string> command)
        {
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null) throw new ArgumentNullException(nameof(pathTemplate));
            if (command == null) throw new ArgumentNullException(nameof(command));

            routes.Add(pathTemplate, new BatchCommandDispatcher(command));
        }

        public static void AddClientBatchCommand(
            this RouteCollection routes,
            string pathTemplate, 
            [NotNull] Action<IBackgroundJobClient, string> command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            routes.AddBatchCommand(pathTemplate, (context, jobId) =>
            {
                var client = context.GetBackgroundJobClient();
                command(client, jobId);
            });
        }

        public static void AddRecurringBatchCommand(
            this RouteCollection routes,
            string pathTemplate,
            [NotNull] Action<IRecurringJobManager, string> command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            routes.AddBatchCommand(pathTemplate, (context, jobId) =>
            {
                var manager = context.GetRecurringJobManager();
                command(manager, jobId);
            });
        }
        
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("For binary compatibility only. Use overload with Action<IRecurringJobManager, string> instead.")]
        public static void AddRecurringBatchCommand(
            this RouteCollection routes,
            string pathTemplate,
            [NotNull] Action<RecurringJobManager, string> command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            routes.AddBatchCommand(pathTemplate, (context, jobId) =>
            {
                var manager = new RecurringJobManager(context.Storage);
                command(manager, jobId);
            });
        }
    }
}
