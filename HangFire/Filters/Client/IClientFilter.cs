// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
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

namespace HangFire.Filters
{
    /// <summary>
    /// Defines methods that are required for a client filter.
    /// </summary>
    public interface IClientFilter
    {
        /// <summary>
        /// Called before the creation of the job. 
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        void OnCreating(CreatingContext filterContext);

        /// <summary>
        /// Called after the creation of the job.
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        void OnCreated(CreatedContext filterContext);
    }
}