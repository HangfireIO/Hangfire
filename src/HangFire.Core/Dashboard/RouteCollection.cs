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
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HangFire.Annotations;

namespace HangFire.Dashboard
{
    public class RouteCollection
    {
        private readonly List<Tuple<string, IRequestDispatcher>> _dispatchers
            = new List<Tuple<string, IRequestDispatcher>>();

        public void Add([NotNull] string pathTemplate, [NotNull] IRequestDispatcher dispatcher)
        {
            if (pathTemplate == null) throw new ArgumentNullException("pathTemplate");
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");

            _dispatchers.Add(new Tuple<string, IRequestDispatcher>(pathTemplate, dispatcher));
        }

        public Tuple<IRequestDispatcher, Match> FindDispatcher(string path)
        {
            if (path.Length == 0) path = "/";

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
                    return new Tuple<IRequestDispatcher, Match>(dispatcher.Item2, match);
                }
            }
            
            return null;
        }
    }
}
