using System.Globalization;
using System.Threading;

namespace HangFire
{
    public class CurrentCultureFilter : JobFilter
    {
        public override void OnJobEnqueueing(JobEnqueueingContext filterContext)
        {
            filterContext.Job["CurrentCulture"] = 
                Thread.CurrentThread.CurrentCulture.Name;
        }

        private CultureInfo _previousCulture;

        public override void OnJobPerforming(JobPerformingContext filterContext)
        {
            var cultureName = filterContext.JobInstance.GetParameter<string>("CurrentCulture");
            var currentThread = Thread.CurrentThread;

            if (cultureName != null)
            {
                _previousCulture = currentThread.CurrentCulture;
                currentThread.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            }
        }

        public override void OnJobPerformed(JobPerformedContext filterContext)
        {
            if (_previousCulture != null)
            {
                Thread.CurrentThread.CurrentCulture = _previousCulture;
            }
        }
    }
}
