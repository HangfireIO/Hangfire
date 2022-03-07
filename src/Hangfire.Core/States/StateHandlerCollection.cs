// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

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

            if (!_handlers.ContainsKey(handler.StateName))
            {
                _handlers.Add(handler.StateName, new List<IStateHandler>());    
            }

            _handlers[handler.StateName].Add(handler);
        }

        public IEnumerable<IStateHandler> GetHandlers(string stateName)
        {
            if (stateName == null || !_handlers.ContainsKey(stateName))
            {
                return Enumerable.Empty<IStateHandler>();
            }

            return _handlers[stateName].ToArray();
        }
    }
}
