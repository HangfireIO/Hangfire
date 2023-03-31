﻿// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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

namespace Hangfire.Server
{
    /// <summary>
    /// Provides the context for the <see cref="IServerFilter.OnPerforming"/>
    /// method of the <see cref="IServerFilter"/> interface.
    /// </summary>
    public class PerformingContext : PerformContext
    {
        public PerformingContext(PerformContext context)
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