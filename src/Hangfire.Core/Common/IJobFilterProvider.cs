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

namespace Hangfire.Common
{
    /// <summary>
    /// Provides an interface for finding filters.
    /// </summary>
    public interface IJobFilterProvider
    {
        /// <summary>
        /// Returns an enumerator that contains all the <see cref="IJobFilterProvider"/>.
        /// </summary>
        ///  
        /// <returns>
        /// The enumerator that contains all the <see cref="IJobFilterProvider"/>.
        /// </returns>
        IEnumerable<JobFilter> GetFilters(Job job);
    }
}
