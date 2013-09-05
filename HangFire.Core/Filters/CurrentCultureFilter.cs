using System.Globalization;
using System.Threading;

namespace HangFire
{
    public class CurrentCultureFilter : IClientFilter, IServerFilter
    {
        public void ClientFilter(ClientFilterContext filterContext)
        {
            var properties = filterContext.JobDescription.Properties;
            properties["CurrentCulture"] = Thread.CurrentThread.CurrentCulture.Name;

            filterContext.EnqueueAction();
        }

        public void ServerFilter(ServerFilterContext filterContext)
        {
            var currentThread = Thread.CurrentThread;
            var prevCulture = currentThread.CurrentCulture;

            var properties = filterContext.JobDescription.Properties;
            var cultureName = properties.ContainsKey("CurrentCulture") ? properties["CurrentCulture"] : null;

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
