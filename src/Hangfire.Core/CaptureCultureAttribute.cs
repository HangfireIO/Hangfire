// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Globalization;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;

namespace Hangfire
{
    public sealed class CaptureCultureAttribute : JobFilterAttribute, IClientFilter, IServerFilter
    {
        public void OnCreating(CreatingContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));

            filterContext.SetJobParameter(
                "CurrentCulture", CultureInfo.CurrentCulture.Name);
            filterContext.SetJobParameter(
                "CurrentUICulture", CultureInfo.CurrentUICulture.Name);
        }

        public void OnCreated(CreatedContext filterContext)
        {
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            var cultureName = filterContext.GetJobParameter<string>("CurrentCulture");
            var uiCultureName = filterContext.GetJobParameter<string>("CurrentUICulture");

            if (!String.IsNullOrEmpty(cultureName))
            {
                filterContext.Items["PreviousCulture"] = CultureInfo.CurrentCulture;
                SetCurrentCulture(new CultureInfo(cultureName));
            }

            if (!String.IsNullOrEmpty(uiCultureName))
            {
                filterContext.Items["PreviousUICulture"] = CultureInfo.CurrentUICulture;
                SetCurrentUICulture(new CultureInfo(uiCultureName));
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
#if NETFULL
            System.Threading.Thread.CurrentThread.CurrentCulture = value;
#else
            CultureInfo.CurrentCulture = value;
#endif
        }

        // ReSharper disable once InconsistentNaming
        private static void SetCurrentUICulture(CultureInfo value)
        {
#if NETFULL
            System.Threading.Thread.CurrentThread.CurrentUICulture = value;
#else
            CultureInfo.CurrentUICulture = value;
#endif
        }
    }
}
