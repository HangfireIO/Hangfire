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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Hangfire.Dashboard
{
    internal class JsonStats : IDashboardDispatcher
    {
        public async Task Dispatch(DashboardContext context)
        {
            var requestedMetrics = await context.Request.GetFormValuesAsync("metrics[]").ConfigureAwait(false);
            var page = new StubPage();
            page.Assign(context);

            var metrics = DashboardMetrics.GetMetrics().Where(x => requestedMetrics.Contains(x.Name));
            var result = new Dictionary<string, Metric>();

            foreach (var metric in metrics)
            {
                var value = metric.Func(page);
                result.Add(metric.Name, value);
            }

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new JsonConverter[]{ new StringEnumConverter { CamelCaseText = true } }
            };
            var serialized = JsonConvert.SerializeObject(result, settings);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(serialized).ConfigureAwait(false);
        }

        private class StubPage : RazorPage
        {
            public override void Execute()
            {
            }
        }
    }
}
