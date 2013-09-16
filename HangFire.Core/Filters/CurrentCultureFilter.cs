using System.Globalization;
using System.Threading;

namespace HangFire
{
    public class CurrentCultureFilter : JobFilter
    {
        public override void OnJobEnqueueing(JobEnqueueingContext filterContext)
        {
            filterContext.JobDescriptor.SetParameter(
                "CurrentCulture", Thread.CurrentThread.CurrentCulture.Name);
        }

        private CultureInfo _previousCulture; // TODO: make it thread-static!

        public override void OnJobPerforming(JobPerformingContext filterContext)
        {
            var cultureName = filterContext.JobDescriptor
                .GetParameter<string>("CurrentCulture");

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
