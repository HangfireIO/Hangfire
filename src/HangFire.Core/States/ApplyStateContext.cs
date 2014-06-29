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
using Hangfire.Storage;

namespace Hangfire.States
{
    public class ApplyStateContext : StateContext
    {
        public ApplyStateContext(StateContext context, IState newState, string oldStateName)
            : base(context)
        {
            if (newState == null) throw new ArgumentNullException("newState");

            OldStateName = oldStateName;
            NewState = newState;
            JobExpirationTimeout = TimeSpan.FromDays(1);
        }
        
        // Hiding the connection from filters, because their methods are being 
        // executed inside a transaction. This property can break them.
        private new IStorageConnection Connection { get { return base.Connection; } }

        public string OldStateName { get; private set; }
        public IState NewState { get; private set; }
        public TimeSpan JobExpirationTimeout { get; set; }
    }
}