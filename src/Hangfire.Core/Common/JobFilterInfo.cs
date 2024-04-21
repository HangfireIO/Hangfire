// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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

using System.Collections.Generic;
using System.Linq;
using Hangfire.Client;
using Hangfire.Server;
using Hangfire.States;

namespace Hangfire.Common
{
    /// <summary>
    /// Encapsulates information about the available job filters.
    /// </summary>
    internal class JobFilterInfo
    {
        private readonly List<IClientFilter> _clientFilters = new List<IClientFilter>();
        private readonly List<IServerFilter> _serverFilters = new List<IServerFilter>();
        private readonly List<IElectStateFilter> _electStateFilters = new List<IElectStateFilter>();
        private readonly List<IApplyStateFilter> _applyStateFilters = new List<IApplyStateFilter>();
        private readonly List<IClientExceptionFilter> _clientExceptionFilters = new List<IClientExceptionFilter>(); 
        private readonly List<IServerExceptionFilter> _serverExceptionFilters = new List<IServerExceptionFilter>();

        /// <summary>
        /// Initializes a new instance of the <see cref="JobFilterInfo"/> class using the specified filters collection.
        /// </summary>
        /// <param name="filters">The filters collection.</param>
        public JobFilterInfo(IEnumerable<JobFilter> filters)
        {
            var list = filters.Select(f => f.Instance).ToList();

            _clientFilters.AddRange(list.OfType<IClientFilter>());
            _serverFilters.AddRange(list.OfType<IServerFilter>());

            _electStateFilters.AddRange(list.OfType<IElectStateFilter>());
            _applyStateFilters.AddRange(list.OfType<IApplyStateFilter>());

            _clientExceptionFilters.AddRange(list.OfType<IClientExceptionFilter>());
            _serverExceptionFilters.AddRange(list.OfType<IServerExceptionFilter>());
        }

        /// <summary>
        /// Gets all the client filters in the application.
        /// </summary>
        /// 
        /// <returns>
        /// The client filters.
        /// </returns>
        public IList<IClientFilter> ClientFilters => _clientFilters;

        /// <summary>
        /// Gets all the server filters in the application.
        /// </summary>
        /// 
        /// <returns>
        /// The server filters.
        /// </returns>
        public IList<IServerFilter> ServerFilters => _serverFilters;

        /// <summary>
        /// Gets all the stat changing filters in the application.
        /// </summary>
        /// 
        /// <returns>
        /// The state changing filters.
        /// </returns>
        public IList<IElectStateFilter> ElectStateFilters => _electStateFilters;

        /// <summary>
        /// Gets all the state changed filters in the application.
        /// </summary>
        /// 
        /// <returns>
        /// The state changed filters.
        /// </returns>
        public IList<IApplyStateFilter> ApplyStateFilters => _applyStateFilters;

        /// <summary>
        /// Gets all the client exception filters in the application.
        /// </summary>
        /// 
        /// <returns>
        /// The client exception filters.
        /// </returns>
        public IList<IClientExceptionFilter> ClientExceptionFilters => _clientExceptionFilters;

        /// <summary>
        /// Gets all the server exception filters in the application.
        /// </summary>
        /// 
        /// <returns>
        /// The server exception filters.
        /// </returns>
        public IList<IServerExceptionFilter> ServerExceptionFilters => _serverExceptionFilters;
    }
}
