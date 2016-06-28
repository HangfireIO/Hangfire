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
using Microsoft.Extensions.Primitives;
#endif

namespace Hangfire.Dashboard
{
    internal class BatchCommandDispatcher : IRequestDispatcher
    {
        private readonly Action<RequestDispatcherContext, string> _command;

        public BatchCommandDispatcher(Action<RequestDispatcherContext, string> command)
        {
            _command = command;
        }

        public async Task Dispatch(RequestDispatcherContext context)
        {
#if NETFULL
            var owinContext = new OwinContext(context.OwinEnvironment);
            var response = owinContext.Response;
            var form = await owinContext.ReadFormSafeAsync();
            var jobIds = form.GetValues("jobs[]");

            if (jobIds == null)
#else
            var response = context.Http.Response;
            var form = await context.Http.Request.ReadFormAsync();
            var jobIds = form["jobs[]"];

            if (jobIds == StringValues.Empty)
#endif
            {
                response.StatusCode = 422;
                return;
            }

            foreach (var jobId in jobIds)
            {
                _command(context, jobId);
            }

            response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
