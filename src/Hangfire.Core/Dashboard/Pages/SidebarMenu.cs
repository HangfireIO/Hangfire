using System;
using System.Collections.Generic;
using Hangfire.Annotations;

namespace Hangfire.Dashboard.Pages
{
    partial class SidebarMenu
    {
        public SidebarMenu([NotNull] IEnumerable<Func<RazorPage, DashboardMenuItem>> items)
        {
            if (items == null) throw new ArgumentNullException("items");
            Items = items;
        }

        public IEnumerable<Func<RazorPage, DashboardMenuItem>> Items { get; private set; }
    }
}
