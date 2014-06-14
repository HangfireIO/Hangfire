using System;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace HangFire.Dashboard
{
    public class DashboardMiddleware : OwinMiddleware
    {
        public DashboardMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public override Task Invoke(IOwinContext context)
        {
            throw new NotImplementedException();
        }
    }
}
