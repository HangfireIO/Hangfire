// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Text.RegularExpressions;

namespace HangFire.Dashboard
{
    public static class RouteCollectionExtensions
    {
        public static void AddRazorPage(
            this RouteCollection routes, 
            string pathTemplate, 
            Func<Match, RazorPage> pageFunc)
        {
            routes.Add(pathTemplate, new RazorPageDispatcher(pageFunc));
        }

        public static void AddCommand(
            this RouteCollection routes,
            string pathTemplate,
            Func<Match, bool> command)
        {
            routes.Add(pathTemplate, new CommandDispatcher(command));
        }

        public static void AddBatchCommand(
            this RouteCollection routes,
            string pathTemplate,
            Action<string> command)
        {
            routes.Add(pathTemplate, new BatchCommandDispatcher(command));
        }
    }
}
