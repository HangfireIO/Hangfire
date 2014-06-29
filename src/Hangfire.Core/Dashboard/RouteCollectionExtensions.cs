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
using System.Text.RegularExpressions;
using Hangfire.Annotations;

namespace Hangfire.Dashboard
{
    public static class RouteCollectionExtensions
    {
        public static void AddRazorPage(
            [NotNull] this RouteCollection routes, 
            [NotNull] string pathTemplate, 
            [NotNull] Func<Match, RazorPage> pageFunc)
        {
            if (routes == null) throw new ArgumentNullException("routes");
            if (pathTemplate == null) throw new ArgumentNullException("pathTemplate");
            if (pageFunc == null) throw new ArgumentNullException("pageFunc");

            routes.Add(pathTemplate, new RazorPageDispatcher(pageFunc));
        }

        public static void AddCommand(
            [NotNull] this RouteCollection routes, 
            [NotNull] string pathTemplate, 
            [NotNull] Func<Match, bool> command)
        {
            if (routes == null) throw new ArgumentNullException("routes");
            if (pathTemplate == null) throw new ArgumentNullException("pathTemplate");
            if (command == null) throw new ArgumentNullException("command");

            routes.Add(pathTemplate, new CommandDispatcher(command));
        }

        public static void AddBatchCommand(
            [NotNull] this RouteCollection routes, 
            [NotNull] string pathTemplate, 
            [NotNull] Action<string> command)
        {
            if (routes == null) throw new ArgumentNullException("routes");
            if (pathTemplate == null) throw new ArgumentNullException("pathTemplate");
            if (command == null) throw new ArgumentNullException("command");

            routes.Add(pathTemplate, new BatchCommandDispatcher(command));
        }
    }
}
