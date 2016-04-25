// This file is part of Hangfire.
// Copyright © 2015 Sergey Odinokov.
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
using Hangfire.Dashboard.Resources;

namespace Hangfire.Dashboard
{
    public class DashboardMetric
    {
        public DashboardMetric(string name, Func<RazorPage, Metric> func)
            : this(name, name, func)
        {
        }

        public DashboardMetric(string name, string titleResourceKey, Func<RazorPage, Metric> func)
        {
            Name = name;
            TitleResourceKey = titleResourceKey;
            Func = func;
        }

        public string Name { get; }
        public Func<RazorPage, Metric> Func { get; }

        public string Title { get { return Strings.ResourceManager.GetString(TitleResourceKey) ?? TitleResourceKey; } }
        public string TitleResourceKey { get; set; }
    }
}