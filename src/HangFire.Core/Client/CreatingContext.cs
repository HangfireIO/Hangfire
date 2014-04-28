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

namespace HangFire.Client
{
    /// <summary>
    /// Provides the context for the <see cref="IClientFilter.OnCreating"/>
    /// method of the <see cref="IClientFilter"/> interface.
    /// </summary>
    public class CreatingContext : CreateContext
    {
        internal CreatingContext(CreateContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="CreatingContext"/>
        /// object was canceled.
        /// </summary>
        public bool Canceled { get; set; }
    }
}