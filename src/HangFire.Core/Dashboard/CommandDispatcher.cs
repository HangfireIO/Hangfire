// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace HangFire.Dashboard
{
    internal class CommandDispatcher : IRequestDispatcher
    {
        private readonly Func<Match, bool> _command;

        public CommandDispatcher(Func<Match, bool> command)
        {
            _command = command;
        }

        public Task Dispatch(IOwinContext context, Match match)
        {
            if (context.Request.Method != WebRequestMethods.Http.Post)
            {
                context.Response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
                return Task.FromResult(false);
            }

            if (_command(match))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            }
            else
            {
                context.Response.StatusCode = 422;
            }

            return Task.FromResult(true);
        }
    }
}
