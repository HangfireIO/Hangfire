using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace HangFire.Web
{
    sealed class DelegatingHttpHandler : IHttpHandler
    {
        private readonly Action<HttpContextBase> _requestProcessor;

        public DelegatingHttpHandler(Action<HttpContextBase> requestProcessor)
        {
            if (requestProcessor == null) throw new ArgumentNullException("requestProcessor");
            _requestProcessor = requestProcessor;
        }

        public void ProcessRequest(HttpContext context)
        {
            _requestProcessor(new HttpContextWrapper(context));
        }

        public bool IsReusable { get { return false; } }
    }
}
