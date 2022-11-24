// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Reflection;
using System.Threading.Tasks;
using Hangfire.Annotations;

namespace Hangfire.Dashboard
{
    internal class CombinedResourceDispatcher : EmbeddedResourceDispatcher
    {
        private readonly IEnumerable<Tuple<Assembly, string>> _resources;

        public CombinedResourceDispatcher(
            [NotNull] string contentType, 
            [NotNull] IEnumerable<Tuple<Assembly, string>> resources)
            : base(contentType, null, null)
        {
            if (resources == null) throw new ArgumentNullException(nameof(resources));
            _resources = resources;
        }

        protected override async Task WriteResponse(DashboardResponse response)
        {
            IEnumerable<Tuple<Assembly, string>> copy;

            lock (_resources)
            {
                copy = _resources.ToArray();
            }

            foreach (var resource in copy)
            {
                await WriteResource(
                    response,
                    resource.Item1,
                    resource.Item2).ConfigureAwait(false);
            }
        }
    }
}
