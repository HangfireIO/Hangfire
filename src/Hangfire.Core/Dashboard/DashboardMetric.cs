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

namespace Hangfire.Dashboard
{
    public class DashboardMetric
    {
        public DashboardMetric(string name, Func<RazorPage, Metric> func) 
            : this(name, name, func)
        {
        }

        public DashboardMetric(string name, string title, Func<RazorPage, Metric> func)
        {
            Name = name;
            Title = title;
            Func = func;
        }

        public string Name { get; }
        public Func<RazorPage, Metric> Func { get; }

        public string Title { get; set; }
        public string Url { get; set; }
    }
}