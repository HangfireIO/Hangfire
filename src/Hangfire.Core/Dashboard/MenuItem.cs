// This file is part of Hangfire. Copyright © 2015 Hangfire OÜ.
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
using System.Linq;
using Hangfire.Annotations;

namespace Hangfire.Dashboard
{
    public class MenuItem
    {
        public MenuItem([NotNull] string text, [NotNull] string url)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Url = url ?? throw new ArgumentNullException(nameof(url));
        }

        [NotNull]
        public string Text { get; }

        [NotNull]
        public string Url { get; }

        public bool Active { get; set; }

        [CanBeNull]
        public DashboardMetric? Metric { get; set; }

        [CanBeNull]
        public DashboardMetric[]? Metrics { get; set; }

        [NotNull]
        public IEnumerable<DashboardMetric> GetAllMetrics()
        {
            if (Metric == null && Metrics == null) return [];

            var metrics = new List<DashboardMetric>();

            if (Metric != null)
            {
                metrics.Add(Metric);
            }

            if (Metrics != null)
            {
                metrics.AddRange(Metrics);
            }

            return metrics.Where(static x => x != null).ToList();
        }
    }
}