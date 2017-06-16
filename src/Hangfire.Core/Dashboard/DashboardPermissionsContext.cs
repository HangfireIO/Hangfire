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
using System.Collections.Generic;
using System.Linq;

namespace Hangfire.Dashboard
{
    /// <summary>
    /// Describes the permissions granted for the current context.
    /// </summary>
    public class DashboardPermissionsContext
    {
        // In case determining authorization is expensive (eg. through a DB call), ensure we
        // calculated it on demand, and once only.
        private readonly IDictionary<DashboardPermission, Lazy<bool>> _permissions;

        public DashboardPermissionsContext(IDashboardContext context)
        {
            _permissions = new Dictionary<DashboardPermission, Lazy<bool>>();

            // For each permission specified in the DashboardOptions, ensure all filters return
            // true in the DashboardContext. For each not specified, assume true.
            foreach (DashboardPermission permission in Enum.GetValues(typeof(DashboardPermission)))
            {
                IEnumerable<IDashboardAuthorizationFilter> filters;
                var authorized = context.Options.Permissions.TryGetValue(permission, out filters)
                    ? new Lazy<bool>(() => filters.All(f => f.Authorize(context))) 
                    : new Lazy<bool>(() => true);
                _permissions.Add(permission, authorized);
            }
        }

        /// <summary>
        /// Determines if the user has read only access to the dashboard.
        /// </summary>
        public bool IsReadOnly => _permissions.Values.All(permission => !permission.Value);

        /// <summary>
        /// Determines if the user is authorized for a specific DashboardPermission.
        /// </summary>
        public bool IsAuthorized(DashboardPermission permission)
        {
            return _permissions[permission].Value;
        }
    }
}