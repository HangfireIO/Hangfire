using System.Collections.Generic;

namespace HangFire.Common.States
{
    public class StateHandlerProviderCollection
    {
        public IEnumerable<StateHandler> GetHandlers()
        {
            return null;
        }
    }

    public static class StateHandlerProviders
    {
        public static StateHandlerProviderCollection Providers { get; private set; }
    }
}
