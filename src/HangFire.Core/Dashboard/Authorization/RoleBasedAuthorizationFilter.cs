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

using System.Linq;
using Microsoft.Owin;

namespace HangFire.Dashboard.Authorization
{
    public class RoleBasedAuthorizationFilter : IAuthorizationFilter
    {
        private readonly bool _blacklist;
        private readonly string[] _roles;

        public RoleBasedAuthorizationFilter(params string[] roles)
            : this(false, roles)
        {
        }

        public RoleBasedAuthorizationFilter(bool blacklist, params string[] roles)
        {
            _blacklist = blacklist;
            _roles = roles;
        }

        public bool Authorize(IOwinContext context)
        {
            if (_blacklist)
            {
                // Blacklist
                return _roles.All(role => !context.Authentication.User.IsInRole(role));
            }

            // Whitelist
            return _roles.Any(role => context.Authentication.User.IsInRole(role));
        }
    }
}
