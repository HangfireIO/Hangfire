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
using System.Web;

namespace HangFire.Web
{
    internal class CommandHandler : GenericHandler
    {
        private readonly Func<bool> _command;

        public CommandHandler(Func<bool> command)
        {
            _command = command;
        }

        public override void ProcessRequest()
        {
            if (Request.HttpMethod != WebRequestMethods.Http.Post)
            {
                throw new HttpException((int)HttpStatusCode.MethodNotAllowed, "Wrong HTTP method.");
            }

            if (_command())
            {
                Response.StatusCode = (int)HttpStatusCode.NoContent;
            }
            else
            {
                Response.StatusCode = 422;
            }
        }
    }
}
