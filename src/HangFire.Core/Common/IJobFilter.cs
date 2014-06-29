// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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

namespace Hangfire.Common
{
    /// <summary>
    /// Defines members that specify the order of filters and 
    /// whether multiple filters are allowed.
    /// </summary>
    public interface IJobFilter
    {
        /// <summary>
        /// When implemented in a class, gets or sets a value 
        /// that indicates whether multiple filters are allowed.
        /// </summary>
        bool AllowMultiple { get; }

        /// <summary>
        /// When implemented in a class, gets the filter order.
        /// </summary>
        int Order { get; }
    }
}