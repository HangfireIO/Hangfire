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
using System.Threading.Tasks;
using HangFire.Annotations;
using Microsoft.Owin;

namespace HangFire.Dashboard
{
    public class DashboardMiddleware : OwinMiddleware
    {
        private readonly RouteCollection _routes;

        public DashboardMiddleware(OwinMiddleware next, [NotNull] RouteCollection routes)
            : base(next)
        {
            if (routes == null) throw new ArgumentNullException("routes");

            _routes = routes;
        }

        public override Task Invoke(IOwinContext context)
        {
            var dispatcher = _routes.FindDispatcher(context.Request.Path.Value);

            return dispatcher != null
                ? dispatcher.Item1.Dispatch(context, dispatcher.Item2)
                : Next.Invoke(context);
        }
    }
}
