using System;
using System.Globalization;
using System.Threading;

namespace HangFire
{
    public class I18NFilter : IServerFilter, IClientFilter
    {
        public void InterceptPerform(HangFireJob job, Action action)
        {
            var currentThread = Thread.CurrentThread;
            var prevCulture = currentThread.CurrentCulture;

            var cultureName = job.Args.ContainsKey("Locale") ? job.Args["Locale"] : null;

            try
            {
                if (cultureName != null)
                {
                    currentThread.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                }

                action();
            }
            finally
            {
                currentThread.CurrentCulture = prevCulture;
            }
        }

        public void InterceptEnqueue(JobDescription jobDescription)
        {
            jobDescription.Args["Locale"] = Thread.CurrentThread.CurrentCulture.Name;
        }
    }
}
