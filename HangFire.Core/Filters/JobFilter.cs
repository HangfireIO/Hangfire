namespace HangFire
{
    public abstract class JobFilter : IClientJobFilter, IServerJobFilter
    {
        public virtual void OnJobEnqueueing(JobEnqueueingContext filterContext)
        {
        }

        public virtual void OnJobEnqueued(JobEnqueuedContext filterContext)
        {
        }

        public virtual void OnJobPerforming(JobPerformingContext filterContext)
        {
        }

        public virtual void OnJobPerformed(JobPerformedContext filterContext)
        {
        }
    }
}
