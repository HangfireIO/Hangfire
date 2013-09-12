using System.Globalization;
using System.Threading;

namespace HangFire
{
    public class CurrentCultureFilter : IClientFilter, IServerFilter
    {
        public void ClientFilter(ClientFilterContext filterContext)
        {
            filterContext.Job["CurrentCulture"] = Thread.CurrentThread.CurrentCulture.Name;

            filterContext.EnqueueAction();
        }

        public void ServerFilter(ServerFilterContext filterContext)
        {
            var currentThread = Thread.CurrentThread;
            var prevCulture = currentThread.CurrentCulture;

            var cultureName = filterContext.JobInstance.Get<string>("CurrentCulture");

            try
            {
                if (cultureName != null)
                {
                    currentThread.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                }

                filterContext.PerformAction();
            }
            finally
            {
                currentThread.CurrentCulture = prevCulture;
            }
        }
    }
}
