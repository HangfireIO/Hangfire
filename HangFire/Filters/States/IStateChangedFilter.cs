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
    /// Provides methods that are required for a state changed filter.
    /// </summary>
    public interface IStateChangedFilter
    {
        /// <summary>
        /// Called after the specified <paramref name="state"/> was applied
        /// to the job within the given <paramref name="transaction"/>.
        /// </summary>
        /// <param name="descriptor">The descriptor of the job, whose state was changed.</param>
        /// <param name="state">The applied state.</param>
        /// <param name="transaction">The current transaction.</param>
        void OnStateApplied(
            JobDescriptor descriptor, JobState state, IRedisTransaction transaction);

        /// <summary>
        /// Called when the state with specified <paramref name="stateName"/> was 
        /// unapplied from the job within the given <paramref name="transaction"/>.
        /// </summary>
        /// <param name="descriptor">The descriptor of the job, whose state is changing.</param>
        /// <param name="stateName">The unapplied state name.</param>
        /// <param name="transaction">The current transaction.</param>
        void OnStateUnapplied(
            JobDescriptor descriptor, string stateName, IRedisTransaction transaction);
    }
}