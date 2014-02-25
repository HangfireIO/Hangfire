// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System.Web;

namespace HangFire.Web
{
    internal abstract class GenericHandler : IHttpHandler
    {
        protected GenericHandler()
        {
            Context = new HttpContextWrapper(HttpContext.Current);
        }

        public HttpApplicationStateBase Application { get { return Context.Application; } }
        public HttpResponseBase Response { get { return Context.Response; } }
        public HttpRequestBase Request { get { return Context.Request; } }
        public HttpServerUtilityBase Server { get { return Context.Server; } }
        public HttpSessionStateBase Session { get { return Context.Session; } }

        public HttpContextBase Context { get; private set; }

        public abstract void ProcessRequest();

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            ProcessRequest();
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
