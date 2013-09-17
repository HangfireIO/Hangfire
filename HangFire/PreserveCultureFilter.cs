using System;
using System.Globalization;
using System.Threading;

using HangFire.Filters;

namespace HangFire
{
    public class PreserveCultureFilter : JobFilter
    {
        public override void OnJobEnqueueing(JobEnqueueingContext filterContext)
        {
            filterContext.JobDescriptor.SetParameter(
                "CurrentCulture", Thread.CurrentThread.CurrentCulture.Name);
            filterContext.JobDescriptor.SetParameter(
                "CurrentUICulture", Thread.CurrentThread.CurrentUICulture.Name);
        }

        private CultureInfo _previousCulture; // TODO: make it thread-static!
        private CultureInfo _previousUICulture;

        public override void OnJobPerforming(JobPerformingContext filterContext)
        {
            var cultureName = filterContext.JobDescriptor
                .GetParameter<string>("CurrentCulture");
            var uiCultureName = filterContext.JobDescriptor
                .GetParameter<string>("CurrentUICulture");

            var currentThread = Thread.CurrentThread;

            if (!String.IsNullOrEmpty(cultureName))
            {
                _previousCulture = currentThread.CurrentCulture;
                currentThread.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            }

            if (!String.IsNullOrEmpty(uiCultureName))
            {
                _previousUICulture = currentThread.CurrentUICulture;
                currentThread.CurrentUICulture = CultureInfo.GetCultureInfo(uiCultureName);
            }
        }

        public override void OnJobPerformed(JobPerformedContext filterContext)
        {
            if (_previousCulture != null)
            {
                Thread.CurrentThread.CurrentCulture = _previousCulture;
            }
            if (_previousUICulture != null)
            {
                Thread.CurrentThread.CurrentUICulture = _previousUICulture;
            }
        }
    }
}
