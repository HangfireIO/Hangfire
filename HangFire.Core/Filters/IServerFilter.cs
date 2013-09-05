namespace HangFire
{
    public interface IServerFilter : IFilter
    {
        void ServerFilter(ServerFilterContext filterContext);
    }
}