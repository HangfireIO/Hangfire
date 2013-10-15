using System;
using System.Globalization;
using System.Threading;

using HangFire.Filters;

namespace HangFire
{
    public class PreserveCultureFilter : JobFilter
    {
        public override void OnCreating(CreatingContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException("filterContext");

            filterContext.JobDescriptor.SetParameter(
                "CurrentCulture", Thread.CurrentThread.CurrentCulture.Name);
            filterContext.JobDescriptor.SetParameter(
                "CurrentUICulture", Thread.CurrentThread.CurrentUICulture.Name);
        }

        public override void OnPerforming(PerformingContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException("filterContext");

            var cultureName = filterContext.JobDescriptor
                .GetParameter<string>("CurrentCulture");
            var uiCultureName = filterContext.JobDescriptor
                .GetParameter<string>("CurrentUICulture");

            var thread = Thread.CurrentThread;

            if (!String.IsNullOrEmpty(cultureName))
            {
                filterContext.Items["PreviousCulture"] = thread.CurrentCulture;
                thread.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            }

            if (!String.IsNullOrEmpty(uiCultureName))
            {
                filterContext.Items["PreviousUICulture"] = thread.CurrentUICulture;
                thread.CurrentUICulture = CultureInfo.GetCultureInfo(uiCultureName);
            }
        }

        public override void OnPerformed(PerformedContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException("filterContext");

            var thread = Thread.CurrentThread;
            if (filterContext.Items.ContainsKey("PreviousCulture"))
            {
                thread.CurrentCulture = (CultureInfo) filterContext.Items["PreviousCulture"];
            }
            if (filterContext.Items.ContainsKey("PreviousUICulture"))
            {
                thread.CurrentUICulture = (CultureInfo) filterContext.Items["PreviousUICulture"];
            }
        }
    }
}
