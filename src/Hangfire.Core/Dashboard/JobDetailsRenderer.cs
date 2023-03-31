// This file is part of Hangfire.
// Copyright © 2020 Hangfire OÜ.
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
using Hangfire.Annotations;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Dashboard
{
    public sealed class JobDetailsRendererDto
    {
        public JobDetailsRendererDto([NotNull] RazorPage page, [NotNull] string jobId, [NotNull] JobDetailsDto jobDetails)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
            JobDetails = jobDetails ?? throw new ArgumentNullException(nameof(jobDetails));
        }
        
        public RazorPage Page { get; }
        public string JobId { get; }
        public JobDetailsDto JobDetails { get; }
    }

    internal static class JobDetailsRenderer
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<Tuple<int, Func<JobDetailsRendererDto, NonEscapedString>>> Renderers = 
            new List<Tuple<int, Func<JobDetailsRendererDto, NonEscapedString>>>();

        public static IEnumerable<Tuple<int, Func<JobDetailsRendererDto, NonEscapedString>>> GetRenderers()
        {
            lock (SyncRoot)
            {
                return Renderers.AsReadOnly();
            }
        }

        public static void AddRenderer(int order, Func<JobDetailsRendererDto, NonEscapedString> renderer)
        {
            lock (SyncRoot)
            {
                Renderers.Add(Tuple.Create(order, renderer));
                Renderers.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            }
        }
    }
}