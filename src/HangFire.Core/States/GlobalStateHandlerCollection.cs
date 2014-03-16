using System;
using System.Collections.Generic;
using HangFire.Common.States;

namespace HangFire.States
{
    public static class GlobalStateHandlerCollection
    {
        private static readonly List<StateHandler> Handlers
            = new List<StateHandler>();

        static GlobalStateHandlerCollection()
        {
            RegisterHandler(new SucceededState.Handler());
            RegisterHandler(new ScheduledState.Handler());
            RegisterHandler(new EnqueuedState.Handler());
        }

        public static void RegisterHandler(StateHandler handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            Handlers.Add(handler);
        }

        public static IEnumerable<StateHandler> GetHandlers()
        {
            return Handlers;
        }
    }
}