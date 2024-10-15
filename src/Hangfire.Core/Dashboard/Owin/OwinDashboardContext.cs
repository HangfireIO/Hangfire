// This file is part of Hangfire. Copyright © 2016 Hangfire OÜ.
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

namespace Hangfire.Dashboard
{
    public sealed class OwinDashboardContext : DashboardContext
    {
        public OwinDashboardContext(
            [NotNull] JobStorage storage,
            [NotNull] DashboardOptions options,
            [NotNull] IDictionary<string, object> environment) 
            : base(storage, options)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));

            Environment = environment;
            Request = new OwinDashboardRequest(environment);
            Response = new OwinDashboardResponse(environment);
        }

        public IDictionary<string, object> Environment { get; }
    }
}
