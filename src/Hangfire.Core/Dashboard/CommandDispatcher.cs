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
    internal class CommandDispatcher : IDashboardDispatcher
    {
        private readonly Func<DashboardContext, bool> _command;

        public CommandDispatcher(Func<DashboardContext, bool> command)
        {
            _command = command;
        }

#if FEATURE_OWIN
        [Obsolete("Use the `CommandDispatcher(Func<DashboardContext, bool>)` ctor instead. Will be removed in 2.0.0.")]
        public CommandDispatcher(Func<RequestDispatcherContext, bool> command)
        {
            _command = context => command(RequestDispatcherContext.FromDashboardContext(context));
        }
#endif

        public Task Dispatch(DashboardContext context)
        {
            if (context.IsReadOnly)
            {
                context.Response.StatusCode = 401;
                return Task.FromResult(false);
            }

            var request = context.Request;
            var response = context.Response;

            if (!"POST".Equals(request.Method, StringComparison.OrdinalIgnoreCase))
            {
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return Task.FromResult(false);
            }

            if (_command(context))
            {
                response.StatusCode = (int)HttpStatusCode.NoContent;
            }
            else
            {
                response.StatusCode = 422;
            }

            return Task.FromResult(true);
        }
    }
}
