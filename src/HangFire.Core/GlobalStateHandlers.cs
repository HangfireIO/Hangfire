using HangFire.Common.States;
using HangFire.States;

namespace HangFire
{
    public static class GlobalStateHandlers
    {
        static GlobalStateHandlers()
        {
            Handlers = new StateHandlerCollection();
            Handlers.AddHandler(new SucceededState.Handler());
            Handlers.AddHandler(new ScheduledState.Handler());
            Handlers.AddHandler(new EnqueuedState.Handler());
        }

        public static StateHandlerCollection Handlers { get; private set; }
    }
}