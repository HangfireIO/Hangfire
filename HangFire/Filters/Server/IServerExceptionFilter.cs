namespace HangFire.Filters
{
    interface IServerExceptionFilter : IJobFilter
    {
        void OnServerException(ServerExceptionContext filterContext);
    }
}
