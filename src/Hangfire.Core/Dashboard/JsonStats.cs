// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Hangfire.Dashboard
{
    internal class JsonStats : IRequestDispatcher
    {
        public async Task Dispatch(RequestDispatcherContext context)
        {
            var owinContext = new OwinContext(context.OwinEnvironment);
            var form = await owinContext.Request.ReadFormAsync();
            var requestedMetrics = new HashSet<string>(form.GetValues("metrics[]") ?? new string[0]);

            var page = new StubPage();
            page.Assign(context);

            var metrics = DashboardMetrics.Metrics.Where(x => requestedMetrics.Contains(x.Name));
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

            owinContext.Response.ContentType = "application/json";
            await owinContext.Response.WriteAsync(serialized);
        }

        private class StubPage : RazorPage
        {
            public override void Execute()
            {
            }
        }
    }
}
