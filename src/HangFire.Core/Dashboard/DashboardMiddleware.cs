using System;
using System.Threading.Tasks;
using HangFire.Annotations;
using Microsoft.Owin;

namespace HangFire.Dashboard
{
    public class DashboardMiddleware : OwinMiddleware
    {
        private readonly DashboardRouteCollection _routes;

        public DashboardMiddleware(OwinMiddleware next, [NotNull] DashboardRouteCollection routes)
            : base(next)
        {
            if (routes == null) throw new ArgumentNullException("routes");

            _routes = routes;
        }

        public override Task Invoke(IOwinContext context)
        {
            var dispatcher = _routes.FindDispatcher(context.Request.Path.Value);

            return dispatcher != null
                ? dispatcher.Item1.Dispatch(context, dispatcher.Item2)
                : Next.Invoke(context);
        }
    }
}
