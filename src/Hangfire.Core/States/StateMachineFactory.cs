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
    public class StateMachineFactory : IStateMachineFactory
    {
        private readonly StateHandlerCollection _handlers;

        public StateMachineFactory(JobStorage storage)
        {
            if (storage == null) throw new ArgumentNullException("storage");

            _handlers = new StateHandlerCollection();
            _handlers.AddRange(GlobalStateHandlers.Handlers);
            _handlers.AddRange(storage.GetStateHandlers());
        }

        public IStateMachine Create(IStorageConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");

            var process = new DefaultStateChangeProcess(_handlers);
            return new StateMachine(connection, process);
        }
    }
}
