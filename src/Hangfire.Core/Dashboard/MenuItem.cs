// This file is part of Hangfire. Copyright © 2015 Sergey Odinokov.
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