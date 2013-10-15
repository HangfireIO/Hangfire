namespace HangFire.Filters
{
    interface IClientExceptionFilter : IJobFilter
    {
        void OnClientException(ClientExceptionContext filterContext);
    }
}