using System.Linq;
using HangFire.States;
using Xunit;

namespace HangFire.Core.Tests
{
    public class GlobalStateHandlersFacts
    {
        [Fact]
        public void AllBasicHandlersShouldBeIncluded()
        {
            var handlerTypes = GlobalStateHandlers.Handlers.Select(x => x.GetType()).ToArray();

            Assert.Contains(typeof(SucceededState.Handler), handlerTypes);
            Assert.Contains(typeof(ScheduledState.Handler), handlerTypes);
            Assert.Contains(typeof(EnqueuedState.Handler), handlerTypes);
            Assert.Contains(typeof(DeletedState.Handler), handlerTypes);
        }
    }
}
