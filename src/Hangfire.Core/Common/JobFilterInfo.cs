// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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
