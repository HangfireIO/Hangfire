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

using System;
using System.Collections.Generic;
using Hangfire.Annotations;

namespace Hangfire.States
{
    public class ApplyStateContext : StateContext
    {
        public ApplyStateContext(
            [NotNull] StateContext context, 
            [NotNull] IState newState, 
            [CanBeNull] string oldStateName, 
            [NotNull] IEnumerable<IState> traversedStates)
            : base(context)
        {
            if (newState == null) throw new ArgumentNullException("newState");
            if (traversedStates == null) throw new ArgumentNullException("traversedStates");

            OldStateName = oldStateName;
            NewState = newState;
            TraversedStates = traversedStates;
            JobExpirationTimeout = TimeSpan.FromDays(1);
        }

        [CanBeNull]
        public string OldStateName { get; private set; }

        [NotNull]
        public IState NewState { get; private set; }
        
        [NotNull]
        public IEnumerable<IState> TraversedStates { get; private set; } 
        
        public TimeSpan JobExpirationTimeout { get; set; }
    }
}