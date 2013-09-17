namespace HangFire.Filters
{
    interface IClientJobExceptionFilter : IJobFilter
    {
        void OnClientException(ClientJobExceptionContext filterContext);
    }
}