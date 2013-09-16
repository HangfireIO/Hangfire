namespace HangFire
{
    public interface IServerJobFilter : IJobFilter
    {
        void OnJobPerforming(JobPerformingContext filterContext);
        void OnJobPerformed(JobPerformedContext filterContext);
    }
}