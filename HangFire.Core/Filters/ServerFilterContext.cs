using System;
using System.Collections.Generic;

namespace HangFire
{
    public class ServerFilterContext
    {
        internal ServerFilterContext(
            string jobId,
            Dictionary<string, string> job,
            object jobInstance,
            Action performAction)
        {
            JobId = jobId;
            Job = job;
            JobInstance = jobInstance;
            PerformAction = performAction;
        }

        public string JobId { get; private set; }
        public Dictionary<string, string> Job { get; private set; }
        public object JobInstance { get; private set; }
        public Action PerformAction { get; private set; }
    }
}