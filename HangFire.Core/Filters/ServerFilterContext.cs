using System;

namespace HangFire
{
    public class ServerFilterContext
    {
        internal ServerFilterContext(
            HangFireJob jobInstance,
            Action performAction)
        {
            JobInstance = jobInstance;
            PerformAction = performAction;
        }

        public HangFireJob JobInstance { get; private set; }
        public Action PerformAction { get; private set; }
    }
}