// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;

namespace HangFire.States
{
    public class StateHandlerCollection
    {
        private readonly Dictionary<string, List<StateHandler>> _handlers = 
            new Dictionary<string, List<StateHandler>>();

        public void AddHandler(StateHandler handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            if (handler.StateName == null) throw new ArgumentException("The StateName property of the given state handler must be non null.", "handler");

            if (!_handlers.ContainsKey(handler.StateName))
            {
                _handlers.Add(handler.StateName, new List<StateHandler>());    
            }

            _handlers[handler.StateName].Add(handler);
        }

        public IEnumerable<StateHandler> GetHandlers(string stateName)
        {
            if (stateName == null || !_handlers.ContainsKey(stateName))
            {
                return Enumerable.Empty<StateHandler>();
            }

            return _handlers[stateName].ToArray();
        }
    }
}
