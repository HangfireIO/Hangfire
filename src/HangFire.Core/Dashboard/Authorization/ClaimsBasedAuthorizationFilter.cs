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
using HangFire.Annotations;
using Microsoft.Owin;

namespace HangFire.Dashboard.Authorization
{
    public class ClaimsBasedAuthorizationFilter : IAuthorizationFilter
    {
        private readonly string _type;
        private readonly string _value;

        public ClaimsBasedAuthorizationFilter([NotNull] string type, [NotNull] string value)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (value == null) throw new ArgumentNullException("value");

            _type = type;
            _value = value;
        }

        public bool Authorize(IOwinContext context)
        {
            return context.Authentication.User.HasClaim(_type, _value);
        }
    }
}
