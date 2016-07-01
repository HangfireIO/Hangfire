// This file is part of Hangfire.
// Copyright © 2016 Sergey Odinokov.
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
using System.Threading.Tasks;
using Hangfire.Annotations;

namespace Hangfire.Dashboard
{
    public class RequestDispatcherWrapper : IDashboardDispatcher
    {
#pragma warning disable 618
        private readonly IRequestDispatcher _dispatcher;
#pragma warning restore 618

#pragma warning disable 618
        public RequestDispatcherWrapper([NotNull] IRequestDispatcher dispatcher)
#pragma warning restore 618
        {
            if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
            _dispatcher = dispatcher;
        }

        public Task Dispatch(DashboardContext context)
        {
#pragma warning disable 618
            return _dispatcher.Dispatch(RequestDispatcherContext.FromDashboardContext(context));
#pragma warning restore 618
        }
    }
}