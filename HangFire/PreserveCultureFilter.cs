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

        public override void OnJobPerforming(JobPerformingContext filterContext)
        {
            var cultureName = filterContext.JobDescriptor
                .GetParameter<string>("CurrentCulture");
            var uiCultureName = filterContext.JobDescriptor
                .GetParameter<string>("CurrentUICulture");

            var currentThread = Thread.CurrentThread;

            if (!String.IsNullOrEmpty(cultureName))
            {
                filterContext.WorkerContext.Items["PreviousCulture"] =
                    currentThread.CurrentCulture;
                currentThread.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            }

            if (!String.IsNullOrEmpty(uiCultureName))
            {
                filterContext.WorkerContext.Items["PreviousUICulture"] =
                    currentThread.CurrentUICulture;
                currentThread.CurrentUICulture = CultureInfo.GetCultureInfo(uiCultureName);
            }
        }

        public override void OnJobPerformed(JobPerformedContext filterContext)
        {
            Thread.CurrentThread.CurrentCulture = (CultureInfo)filterContext.WorkerContext.Items["PreviousCulture"];
            Thread.CurrentThread.CurrentUICulture = (CultureInfo)filterContext.WorkerContext.Items["PreviousUICulture"];
        }
    }
}
