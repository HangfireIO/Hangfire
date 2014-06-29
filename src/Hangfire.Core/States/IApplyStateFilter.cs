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

using Hangfire.Storage;

namespace Hangfire.States
{
    /// <summary>
    /// Provides methods that are required for a state changed filter.
    /// </summary>
    public interface IApplyStateFilter
    {
        /// <summary>
        /// Called after the specified state was applied
        /// to the job within the given transaction.
        /// </summary>
        void OnStateApplied(
            ApplyStateContext context, IWriteOnlyTransaction transaction);

        /// <summary>
        /// Called when the state with specified state was 
        /// unapplied from the job within the given transaction.
        /// </summary>
        void OnStateUnapplied(
            ApplyStateContext context, IWriteOnlyTransaction transaction);
    }
}