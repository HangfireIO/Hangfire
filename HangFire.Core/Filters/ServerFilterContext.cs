using System;

namespace HangFire
{
    public class ServerFilterContext
    {
        internal ServerFilterContext(object job, JobDescription jobDescription, Action performAction)
        {
            Job = job;
            JobDescription = jobDescription;
            PerformAction = performAction;
        }

        public object Job { get; private set; }
        public JobDescription JobDescription { get; private set; }
        public Action PerformAction { get; private set; }
    }
}