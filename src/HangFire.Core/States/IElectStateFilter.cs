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

namespace Hangfire.States
{
    /// <summary>
    /// Defines methods that are required for a state changing filter.
    /// </summary>
    public interface IElectStateFilter
    {
        /// <summary>
        /// Called when the current state of the job is being changed to the
        /// specified candidate state.
        /// This state change could be intercepted and the final state could
        /// be changed through setting the different state in the context 
        /// in an implementation of this method.
        /// </summary>
        void OnStateElection(ElectStateContext context);
    }
}