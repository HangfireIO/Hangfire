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

namespace Hangfire
{
    /// <summary>
    /// Specifies a candidate state for a background job that will be chosen
    /// by the <see cref="AutomaticRetryAttribute"/> filter after exceeding
    /// the number of retry attempts.
    /// </summary>
    public enum AttemptsExceededAction
    {
        /// <summary>
        /// Background job will be moved to the <see cref="States.FailedState"/>.
        /// </summary>
        Fail = 0,

        /// <summary>
        /// Background job will be moved to the <see cref="States.DeletedState"/>.
        /// </summary>
        Delete
    }
}