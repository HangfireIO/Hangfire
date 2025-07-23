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
using Hangfire.Annotations;

// ReSharper disable RedundantNullnessAttributeWithNullableReferenceTypes
#nullable enable

namespace Hangfire.Dashboard
{
    public class DashboardMetric
    {
        public DashboardMetric([NotNull] string name, [NotNull] Func<RazorPage, Metric> func) 
            : this(name, name, func)
        {
        }

        public DashboardMetric([NotNull] string name, [NotNull] string title, [NotNull] Func<RazorPage, Metric?> func)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Func = func ?? throw new ArgumentNullException(nameof(func));
        }

        [NotNull]
        public string Name { get; }

        [NotNull]
        public Func<RazorPage, Metric?> Func { get; }

        [NotNull]
        public string Title { get; set; }

        [CanBeNull]
        public string? Url { get; set; }
    }
}