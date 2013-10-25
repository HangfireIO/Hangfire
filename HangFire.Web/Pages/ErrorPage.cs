using System;

namespace HangFire.Web.Pages
{
    partial class ErrorPage
    {
        public ErrorPage(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }
    }
}
