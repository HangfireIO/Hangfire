using HangFire.Client;

namespace HangFire.States
{
    public class StateContext
    {
        internal StateContext(StateContext context)
            : this(context.JobId, context.JobMethod)
        {
        }

        internal StateContext(string jobId, JobMethod jobMethod)
        {
            JobId = jobId;
            JobMethod = jobMethod;
        }

        public string JobId { get; private set; }
        public JobMethod JobMethod { get; private set; }
    }
}