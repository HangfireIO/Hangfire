namespace HangFire
{
    interface IJobExceptionFilter : IJobFilter
    {
        void OnServerException(ServerJobExceptionContext filterContext);
        void OnClientException(ClientJobExceptionContext filterContext);
    }
}
