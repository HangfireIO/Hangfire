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
    /// Provides a mechanism for performing custom actions when applying or
    /// unapplying the state of a background job by <see cref="StateMachine"/>.
    /// </summary>
    public interface IStateHandler
    {
        /// <summary>
        /// Gets the name of a state, for which custom actions will be
        /// performed.
        /// </summary>
        string StateName { get; }

        /// <summary>
        /// Performs additional actions when applying a state whose name is
        /// equal to the <see cref="StateName"/> property.
        /// </summary>
        /// <param name="context">The context of a state applying process.</param>
        /// <param name="transaction">The current transaction of a state applying process.</param>
        void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction);

        /// <summary>
        /// Performs additional actions when unapplying a state whose name
        /// is equal to the <see cref="StateName"/> property.
        /// </summary>
        /// <param name="context">The context of a state applying process.</param>
        /// <param name="transaction">The current transaction of a state applying process.</param>
        void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction);
    }
}