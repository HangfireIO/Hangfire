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

using HangFire.States;
using ServiceStack.Redis;

namespace HangFire.Filters
{
    /// <summary>
    /// Defines methods that are required for a state changing filter.
    /// </summary>
    public interface IStateChangingFilter
    {
        /// <summary>
        /// Called when the current state of the job is being changed to the
        /// specified candidate <paramref name="state"/>.
        /// This state change could be intercepted and the final state could
        /// be changed through returning the different state in the implementation
        /// of this method.
        /// </summary>
        /// <param name="descriptor">The descriptor of the job, whose state is changing.</param>
        /// <param name="state">The current state candidate.</param>
        /// <returns>The new state candidate, which will be applied to the job.</returns>
        /// <param name="redis">Redis connection.</param>
        JobState OnStateChanging(
            JobDescriptor descriptor, JobState state, IRedisClient redis);
    }
}