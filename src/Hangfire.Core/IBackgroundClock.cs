// This file is part of Hangfire. Copyright © 2025 Hangfire OÜ.
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

namespace Hangfire
{
    /// <summary>
    /// Provides method to retrieve the current time in UTC format.
    /// </summary>
    public interface IBackgroundClock
    {
        /// <summary>
        /// Returns the current time in UTC format.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> instance with <see cref="DateTimeKind.Utc"/> specified.</returns>
        DateTime GetCurrentTime();
    }

    internal sealed class CallbackBackgroundClock(Func<DateTime> nowFactory) : IBackgroundClock
    {
        private readonly Func<DateTime> _callback = nowFactory ?? throw new ArgumentNullException(nameof(nowFactory));

        public DateTime GetCurrentTime()
        {
            return _callback();
        }
    }
}