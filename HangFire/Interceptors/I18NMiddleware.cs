using System;
using System.Globalization;
using System.Threading;

namespace HangFire.Interceptors
{
    public class I18NInterceptor : IPerformInterceptor, IEnqueueInterceptor
    {
        public void InterceptPerform(Worker worker, Action action)
        {
            var currentThread = Thread.CurrentThread;
            var prevCulture = currentThread.CurrentCulture;

            var cultureName = worker.Args.ContainsKey("Locale") ? worker.Args["Locale"] : null;

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

        public void InterceptEnqueue(Job job)
        {
            job.Args["Locale"] = Thread.CurrentThread.CurrentCulture.Name;
        }
    }
}
