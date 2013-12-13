// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Globalization;
using System.Threading;

using HangFire.Filters;

namespace HangFire
{
    public class PreserveCultureAttribute : JobFilterAttribute, IClientFilter, IServerFilter
    {
        public void OnCreating(CreatingContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException("filterContext");

            filterContext.JobDescriptor.SetParameter(
                "CurrentCulture", Thread.CurrentThread.CurrentCulture.Name);
            filterContext.JobDescriptor.SetParameter(
                "CurrentUICulture", Thread.CurrentThread.CurrentUICulture.Name);
        }

        public void OnCreated(CreatedContext filterContext)
        {
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException("filterContext");

            var cultureName = filterContext
                .GetParameter<string>("CurrentCulture");
            var uiCultureName = filterContext
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

        public void OnPerformed(PerformedContext filterContext)
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
