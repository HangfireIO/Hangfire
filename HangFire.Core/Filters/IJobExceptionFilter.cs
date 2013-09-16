namespace HangFire
{
    interface IJobExceptionFilter : IJobFilter
    {
        void OnException(JobExceptionContext filterContext);
    }
}
