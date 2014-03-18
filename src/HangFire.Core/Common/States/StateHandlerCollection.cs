using System;
using System.Collections.Generic;
using System.Linq;

namespace HangFire.Common.States
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
