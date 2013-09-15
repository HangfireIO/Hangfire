using System.Web;

namespace HangFire.Web
{
    public abstract class GenericHandler : IHttpHandler
    {
        protected GenericHandler()
        {
            Context = new HttpContextWrapper(HttpContext.Current);
        }

        public HttpApplicationStateBase Application { get { return Context.Application; } }
        public HttpResponseBase Response { get { return Context.Response; } }
        public HttpRequestBase Request { get { return Context.Request; } }
        public HttpServerUtilityBase Server { get { return Context.Server; } }
        public HttpSessionStateBase Session { get { return Context.Session; } }

        public HttpContextBase Context { get; private set; }

        public abstract void ProcessRequest();

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            ProcessRequest();
        }

        bool IHttpHandler.IsReusable
        {
            get { return false; }
        }
    }
}
