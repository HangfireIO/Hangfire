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
