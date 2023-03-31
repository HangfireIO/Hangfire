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
using System.Text.RegularExpressions;
using Hangfire.Annotations;

namespace Hangfire.Dashboard
{
    public class RouteCollection
    {
        private readonly List<Tuple<string, IDashboardDispatcher>> _dispatchers
            = new List<Tuple<string, IDashboardDispatcher>>();

#if FEATURE_OWIN
        [Obsolete("Use the Add(string, IDashboardDispatcher) overload instead. Will be removed in 2.0.0.")]
        public void Add([NotNull] string pathTemplate, [NotNull] IRequestDispatcher dispatcher)
        {
            if (pathTemplate == null) throw new ArgumentNullException(nameof(pathTemplate));
            if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));

            _dispatchers.Add(new Tuple<string, IDashboardDispatcher>(pathTemplate, new RequestDispatcherWrapper(dispatcher)));
        }
#endif

        public void Add([NotNull] string pathTemplate, [NotNull] IDashboardDispatcher dispatcher)
        {
            if (pathTemplate == null) throw new ArgumentNullException(nameof(pathTemplate));
            if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));

            _dispatchers.Add(new Tuple<string, IDashboardDispatcher>(pathTemplate, dispatcher));
        }

        public Tuple<IDashboardDispatcher, Match> FindDispatcher(string path)
        {
            if (path.Length == 0) path = "/";
            else if (path.Length > 1) path = path.TrimEnd('/');

            foreach (var dispatcher in _dispatchers)
            {
                var pattern = dispatcher.Item1;

                if (!pattern.StartsWith("^", StringComparison.OrdinalIgnoreCase))
                    pattern = "^" + pattern;
                if (!pattern.EndsWith("$", StringComparison.OrdinalIgnoreCase))
                    pattern += "$";

                var match = Regex.Match(
                    path,
                    pattern,
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (match.Success)
                {
                    return new Tuple<IDashboardDispatcher, Match>(dispatcher.Item2, match);
                }
            }
            
            return null;
        }
    }
}
