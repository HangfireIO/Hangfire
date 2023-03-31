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
using System.Net;
using System.Threading.Tasks;

namespace Hangfire.Dashboard
{
    internal class BatchCommandDispatcher : IDashboardDispatcher
    {
        private readonly Action<DashboardContext, string> _command;

        public BatchCommandDispatcher(Action<DashboardContext, string> command)
        {
            _command = command;
        }

#if FEATURE_OWIN
        [Obsolete("Use the `BatchCommandDispatcher(Action<DashboardContext>, string)` instead. Will be removed in 2.0.0.")]
        public BatchCommandDispatcher(Action<RequestDispatcherContext, string> command)
        {
            _command = (context, jobId) => command(RequestDispatcherContext.FromDashboardContext(context), jobId);
        }
#endif

        public async Task Dispatch(DashboardContext context)
        {
            if (context.IsReadOnly)
            {
                context.Response.StatusCode = 401;
                return;
            }

            var jobIds = await context.Request.GetFormValuesAsync("jobs[]").ConfigureAwait(false);
            if (jobIds.Count == 0)
            {
                context.Response.StatusCode = 422;
                return;
            }

            foreach (var jobId in jobIds)
            {
                _command(context, jobId);
            }

            context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
