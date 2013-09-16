namespace HangFire
{
    public interface IClientJobFilter : IJobFilter
    {
        void OnJobEnqueueing(JobEnqueueingContext filterContext);
        void OnJobEnqueued(JobEnqueuedContext filterContext);
    }
}