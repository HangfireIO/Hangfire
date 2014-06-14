using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HangFire.Annotations;

namespace HangFire.Dashboard
{
    public class DashboardRouteCollection
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
