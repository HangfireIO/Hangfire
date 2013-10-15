namespace HangFire.Filters
{
    public abstract class JobFilter : IClientFilter, IServerFilter
    {
        public virtual void OnCreating(CreatingContext filterContext)
        {
        }

        public virtual void OnCreated(CreatedContext filterContext)
        {
        }

        public virtual void OnPerforming(PerformingContext filterContext)
        {
        }

        public virtual void OnPerformed(PerformedContext filterContext)
        {
        }
    }
}
