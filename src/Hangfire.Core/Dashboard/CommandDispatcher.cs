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
using System.Net;
using System.Threading.Tasks;
#if NETFULL
using Microsoft.Owin;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace Hangfire.Dashboard
{
    internal class CommandDispatcher : IRequestDispatcher
    {
        private readonly Func<RequestDispatcherContext, bool> _command;

        public CommandDispatcher(Func<RequestDispatcherContext, bool> command)
        {
            _command = command;
        }

        public Task Dispatch(RequestDispatcherContext context)
        {
#if NETFULL
            var owinContext = new OwinContext(context.OwinEnvironment);
            var request = owinContext.Request;
            var response = owinContext.Response;
#else
            var request = context.Http.Request;
            var response = context.Http.Response;
#endif

            if ("POST".Equals(request.Method, StringComparison.OrdinalIgnoreCase))
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
