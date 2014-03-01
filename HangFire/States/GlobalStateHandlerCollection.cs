using System;
using System.Collections.Generic;
using HangFire.Common.States;

namespace HangFire.States
{
    public static class GlobalStateHandlerCollection
    {
        private static readonly List<JobStateHandler> Handlers
            = new List<JobStateHandler>();

        static GlobalStateHandlerCollection()
        {
            RegisterHandler(new SucceededState.Handler());
            RegisterHandler(new ScheduledState.Handler());
            RegisterHandler(new EnqueuedState.Handler());
        }

        public static void RegisterHandler(JobStateHandler handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            Handlers.Add(handler);
        }

        public static IEnumerable<JobStateHandler> GetHandlers()
        {
            return Handlers;
        }
    }
}