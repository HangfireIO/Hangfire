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

using System.Collections.Generic;
using System.Linq;

namespace Hangfire.Dashboard
{
    public class MenuItem
    {
        public MenuItem(string text, string url)
        {
            Text = text;
            Url = url;
        }

        public string Text { get; }
        public string Url { get; }

        public bool Active { get; set; }
        public DashboardMetric Metric { get; set; }
        public DashboardMetric[] Metrics { get; set; }

        public IEnumerable<DashboardMetric> GetAllMetrics()
        {
            var metrics = new List<DashboardMetric> { Metric };
            
            if (Metrics != null)
            {
                metrics.AddRange(Metrics);
            }

            return metrics.Where(x => x != null).ToList();
        }
    }
}