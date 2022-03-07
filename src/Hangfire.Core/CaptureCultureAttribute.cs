// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;
using System.Globalization;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire
{
    public sealed class CaptureCultureAttribute : JobFilterAttribute, IClientFilter, IServerFilter
    {
        private readonly ILog _logger = LogProvider.GetLogger(typeof(CaptureCultureAttribute));

        public void OnCreating(CreatingContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));

            filterContext.SetJobParameter("CurrentCulture", CultureInfo.CurrentCulture.Name);
            filterContext.SetJobParameter("CurrentUICulture", CultureInfo.CurrentUICulture.Name);
        }

        public void OnCreated(CreatedContext filterContext)
        {
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            var cultureName = filterContext.GetJobParameter<string>("CurrentCulture");
            var uiCultureName = filterContext.GetJobParameter<string>("CurrentUICulture");

            try
            {
                if (cultureName != null)
                {
                    filterContext.Items["PreviousCulture"] = CultureInfo.CurrentCulture;
                    SetCurrentCulture(new CultureInfo(cultureName));
                }
            }
            catch (CultureNotFoundException ex)
            {
                // TODO: Make this overridable, and start with throwing an exception
                _logger.WarnException($"Unable to set CurrentCulture for job {filterContext.BackgroundJob.Id} due to an exception", ex);
            }

            try
            {
                if (uiCultureName != null)
                {
                    filterContext.Items["PreviousUICulture"] = CultureInfo.CurrentUICulture;
                    SetCurrentUICulture(new CultureInfo(uiCultureName));
                }
            }
            catch (CultureNotFoundException ex)
            {
                // TODO: Make this overridable, and start with throwing an exception
                _logger.WarnException($"Unable to set CurrentUICulture for job {filterContext.BackgroundJob.Id} due to an exception", ex);
            }
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));

            if (filterContext.Items.ContainsKey("PreviousCulture"))
            {
                SetCurrentCulture((CultureInfo) filterContext.Items["PreviousCulture"]);
            }
            if (filterContext.Items.ContainsKey("PreviousUICulture"))
            {
                SetCurrentUICulture((CultureInfo)filterContext.Items["PreviousUICulture"]);
            }
        }
        
        private static void SetCurrentCulture(CultureInfo value)
        {
#if !NETSTANDARD1_3
            System.Threading.Thread.CurrentThread.CurrentCulture = value;
#else
            CultureInfo.CurrentCulture = value;
#endif
        }

        // ReSharper disable once InconsistentNaming
        private static void SetCurrentUICulture(CultureInfo value)
        {
#if !NETSTANDARD1_3
            System.Threading.Thread.CurrentThread.CurrentUICulture = value;
#else
            CultureInfo.CurrentUICulture = value;
#endif
        }
    }
}
