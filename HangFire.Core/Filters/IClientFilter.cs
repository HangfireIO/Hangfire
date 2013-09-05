namespace HangFire
{
    public interface IClientFilter : IFilter
    {
        void ClientFilter(ClientFilterContext filterContext);
    }
}