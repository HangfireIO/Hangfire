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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Dashboard
{
    internal class JsonStatus : IRequestDispatcher
    {
        private static readonly TimeSpan ServerTimeout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan ResultCacheTimeout = TimeSpan.FromMinutes(0.5);

        private const string EmptyResult = "[]";

        private static object ResultCacheLock = new object();
        private static string ResultCache = null;
        private static DateTime ResultCacheLastUpdate = DateTime.MinValue;

        public async Task Dispatch(RequestDispatcherContext context)
        {
            DateTime now = DateTime.UtcNow;

            string results = ResultCache;
            if (string.IsNullOrWhiteSpace(results) || ResultCacheLastUpdate < now.Subtract(ResultCacheTimeout))
            {
                lock (ResultCacheLock)
                {
                    if (string.IsNullOrWhiteSpace(results) || ResultCacheLastUpdate < now.Subtract(ResultCacheTimeout))
                    {
                        results = ResultCache = GetSerialisedStatusResponse(context, now);
                        ResultCacheLastUpdate = now;
                    }
                }
            }

            OwinContext owinContext = new OwinContext(context.OwinEnvironment);
            owinContext.Response.ContentType = "application/json";
            owinContext.Response.StatusCode = !string.IsNullOrWhiteSpace(results) && results != EmptyResult
                ? 200
                : 500;

            await owinContext.Response.WriteAsync(results);

            /*DateTime now = DateTime.UtcNow;

            StubPage page = new StubPage();
            page.Assign(context);

            IMonitoringApi monitoringAPI = page.Storage.GetMonitoringApi();
            List<ServerDto> servers = monitoringAPI
                .Servers()
                .Where(s => s.Heartbeat.HasValue && s.Heartbeat.Value >= now.Subtract(ServerTimeout))
                .ToList();

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new JsonConverter[]
                {
                    new StringEnumConverter
                    {
                        CamelCaseText = true
                    }
                }
            };
            string serialized = JsonConvert.SerializeObject(servers, jsonSerializerSettings);

            OwinContext owinContext = new OwinContext(context.OwinEnvironment);
            owinContext.Response.ContentType = "application/json";
            owinContext.Response.StatusCode = servers.Any()
                ? 200
                : 500;

            await owinContext.Response.WriteAsync(serialized);*/
        }

        private string GetSerialisedStatusResponse(RequestDispatcherContext context, DateTime now)
        {
            StubPage page = new StubPage();
            page.Assign(context);

            IMonitoringApi monitoringAPI = page.Storage.GetMonitoringApi();
            List<ServerDto> servers = monitoringAPI
                .Servers()
                .Where(s => s.Heartbeat.HasValue && s.Heartbeat.Value >= now.Subtract(ServerTimeout))
                .ToList();

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new JsonConverter[]
                {
                    new StringEnumConverter
                    {
                        CamelCaseText = true
                    }
                }
            };
            return JsonConvert.SerializeObject(servers, jsonSerializerSettings);
        }

        private class StubPage : RazorPage
        {
            public override void Execute()
            {
            }
        }
    }
}
