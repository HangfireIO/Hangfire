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
using System.Threading.Tasks;
using Hangfire.Annotations;

namespace Hangfire.Dashboard
{
    internal sealed class RazorPageDispatcher : IDashboardDispatcher
    {
        private readonly Func<Match, RazorPage> _pageFunc;

        public RazorPageDispatcher([NotNull] Func<Match, RazorPage> pageFunc)
        {
            _pageFunc = pageFunc ?? throw new ArgumentNullException(nameof(pageFunc));
        }

        public Task Dispatch(DashboardContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            context.Response.ContentType = "text/html";

            var page = _pageFunc(context.UriMatch);
            page.Assign(context);

            return context.Response.WriteAsync(page.ToString());
        }
    }
}
