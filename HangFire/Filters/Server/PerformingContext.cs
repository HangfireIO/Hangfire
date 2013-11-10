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

using HangFire.Server;

namespace HangFire.Filters
{
    /// <summary>
    /// Provides the context for the <see cref="IServerFilter.OnPerforming"/>
    /// method of the <see cref="IServerFilter"/> interface.
    /// </summary>
    public class PerformingContext : PerformContext
    {
        internal PerformingContext(
            PerformContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="PerformingContext"/>
        /// object was canceled.
        /// </summary>
        public bool Canceled { get; set; }
    }
}