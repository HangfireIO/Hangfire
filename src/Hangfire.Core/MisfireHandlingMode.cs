// This file is part of Hangfire.
// Copyright © 2021 Sergey Odinokov.
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
    /// Specifies how to handle missed schedule when processing server was
    /// inactive.
    /// </summary>
    public enum MisfireHandlingMode
    {
        /// <summary>
        /// Default mode. Specifies that only a single background job will
        /// be created, no matter how many occurrences were missed. The "Time"
        /// parameter for the background job will point to the time background
        /// job was scheduled.
        /// </summary>
        Relaxed = 0,

        /// <summary>
        /// Specifies that new background job will be created for every missed
        /// occurrence, with "Time" parameter set to the corresponding schedule
        /// time. 
        /// </summary>
        Strict  = 1,
    }
}