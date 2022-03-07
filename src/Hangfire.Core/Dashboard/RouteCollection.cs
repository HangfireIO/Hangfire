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
