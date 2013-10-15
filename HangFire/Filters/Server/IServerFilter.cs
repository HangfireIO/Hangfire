namespace HangFire.Filters
{
    public interface IServerFilter : IJobFilter
    {
        void OnPerforming(PerformingContext filterContext);
        void OnPerformed(PerformedContext filterContext);
    }
}