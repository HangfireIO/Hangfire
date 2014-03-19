using System.Collections.Generic;
using HangFire.Common.States;
using HangFire.States;

namespace HangFire
{
    public static class GlobalStateHandlers
    {
        static GlobalStateHandlers()
        {
            Handlers = new List<StateHandler>();
            Handlers.Add(new SucceededState.Handler());
            Handlers.Add(new ScheduledState.Handler());
            Handlers.Add(new EnqueuedState.Handler());
        }

        public static ICollection<StateHandler> Handlers { get; private set; }
    }
}