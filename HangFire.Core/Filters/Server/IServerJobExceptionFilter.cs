namespace HangFire.Filters
{
    interface IServerJobExceptionFilter : IJobFilter
    {
        void OnServerException(ServerJobExceptionContext filterContext);
    }
}
