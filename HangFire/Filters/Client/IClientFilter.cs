namespace HangFire.Filters
{
    public interface IClientFilter : IJobFilter
    {
        void OnCreating(CreatingContext filterContext);
        void OnCreated(CreatedContext filterContext);
    }
}