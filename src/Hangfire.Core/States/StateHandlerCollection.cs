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
using System.Linq;

namespace Hangfire.States
{
    public class StateHandlerCollection
    {
        private readonly Dictionary<string, List<IStateHandler>> _handlers = 
            new Dictionary<string, List<IStateHandler>>();

        public void AddRange(IEnumerable<IStateHandler> handlers)
        {
            if (handlers == null) throw new ArgumentNullException(nameof(handlers));

            foreach (var handler in handlers)
            {
                AddHandler(handler);
            }
        }

        public void AddHandler(IStateHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (handler.StateName == null) throw new ArgumentException("The StateName property of the given state handler must be non null.", nameof(handler));

            if (!_handlers.TryGetValue(handler.StateName, out var stateHandlers))
            {
                stateHandlers = new List<IStateHandler>();
                _handlers.Add(handler.StateName, stateHandlers);
            }

            stateHandlers.Add(handler);
        }

        public IEnumerable<IStateHandler> GetHandlers(string stateName)
        {
            if (stateName == null || !_handlers.TryGetValue(stateName, out var stateHandlers))
            {
                return Enumerable.Empty<IStateHandler>();
            }

            return stateHandlers.ToArray();
        }
    }
}
