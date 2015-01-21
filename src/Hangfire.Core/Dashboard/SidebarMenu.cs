using System;
using System.Collections.Generic;
using System.Text;
using Hangfire.Annotations;
using Hangfire.Storage.Monitoring;

namespace Hangfire.Dashboard
{
    public static class SidebarMenu
    {
        private static readonly List<Func<RazorPage, JobStorage, StatisticsDto, string>> ItemFuncList
            = new List<Func<RazorPage, JobStorage, StatisticsDto, string>>();

        static SidebarMenu()
        {
            // Dashboard
            AddItem((page, storage, stats) => String.Format(
                @"<a class=""list-group-item {0}"" href=""{1}"">
    <span class=""glyphicon glyphicon-dashboard""></span>
    Dashboard
</a>",
                page.RequestPath.Equals("/") || page.RequestPath.Length == 0 ? "active" : null,
                page.LinkTo("/")));

            // Servers
            AddItem((page, storage, stats) => String.Format(
                @"<a class=""list-group-item {0}"" href=""{1}"">
    <span id=""stats-servers"" class=""label label-default pull-right"">{2}</span>
    <span class=""glyphicon glyphicon-hdd""></span>
    Servers
</a>",
                page.RequestPath.Equals("/servers") ? "active" : null,
                page.LinkTo("/servers"),
                stats.Servers));

            // Recurring jobs
            AddItem((page, storage, stats) => String.Format(
                @"<a class=""list-group-item {0}"" href=""{1}"">
    <span id=""stats-recurring"" class=""label label-default pull-right"">{2}</span>
    <span class=""glyphicon glyphicon-time""></span>
    Recurring jobs
</a>",
                page.RequestPath.Equals("/recurring") ? "active" : null,
                page.LinkTo("/recurring"),
                stats.Recurring));

            // Queues
            AddItem((page, storage, stats) => String.Format(
                @"<a class=""list-group-item {0}"" href=""{1}"">
    <span class=""label label-default pull-right"">
        <span id=""stats-enqueued"" title=""Enqueued jobs count"">{2}</span>
        / 
        <span id=""stats-queues"" title=""Queues count"">{3}</span>
    </span>
    <span class=""glyphicon glyphicon-inbox""></span>
    Queues
</a>",
                page.RequestPath.StartsWith("/queues") ? "active" : null,
                page.LinkTo("/queues"),
                stats.Enqueued,
                stats.Queues));

            // Scheduled
            AddItem((page, storage, stats) => String.Format(
                @"<a class=""list-group-item stats-indent {0}"" href=""{1}"">
    <span id=""stats-scheduled"" class=""label label-info pull-right"">{2}</span>
    Scheduled
</a>",
                page.RequestPath.Equals("/scheduled") ? "active" : null,
                page.LinkTo("/scheduled"),
                stats.Scheduled));

            // Processing
            AddItem((page, storage, stats) => String.Format(
                @"<a class=""list-group-item stats-indent {0}"" href=""{1}"">
    <span id=""stats-processing"" class=""label label-warning pull-right"">{2}</span>
    Processing
</a>",
                page.RequestPath.Equals("/processing") ? "active" : null,
                page.LinkTo("/processing"),
                stats.Processing));

            // Succeeded
            AddItem((page, storage, stats) => String.Format(
                @"<a class=""list-group-item stats-indent {0}"" href=""{1}"">
    <span id=""stats-succeeded"" class=""label label-success pull-right"">{2}</span>
    Succeeded
</a>",
                page.RequestPath.Equals("/succeeded") ? "active" : null,
                page.LinkTo("/succeeded"),
                stats.Succeeded));

            // Failed
            AddItem((page, storage, stats) => String.Format(
                @"<a class=""list-group-item stats-indent {0}"" href=""{1}"">
    <span id=""stats-failed"" class=""label label-danger pull-right"">{2}</span>
    Failed
</a>",
                page.RequestPath.Equals("/failed") ? "active" : null,
                page.LinkTo("/failed"),
                stats.Failed));

            // Deleted
            AddItem((page, storage, stats) => String.Format(
                @"<a class=""list-group-item stats-indent {0}"" href=""{1}"">
    <span id=""stats-deleted"" class=""label label-default pull-right"">{2}</span>
    Deleted
</a>",
                page.RequestPath.Equals("/deleted") ? "active" : null,
                page.LinkTo("/deleted"),
                stats.Deleted));
        }

        public static void AddItem([NotNull] Func<RazorPage, JobStorage, string> itemFunc)
        {
            if (itemFunc == null) throw new ArgumentNullException("itemFunc");

            AddItem((page, storage, statistics) => itemFunc(page, storage));
        }

        internal static void AddItem([NotNull] Func<RazorPage, JobStorage, StatisticsDto, string> itemFunc)
        {
            if (itemFunc == null) throw new ArgumentNullException("itemFunc");

            lock (ItemFuncList)
            {
                ItemFuncList.Add(itemFunc);
            }
        }

        internal static NonEscapedString Render([NotNull] RazorPage page, [NotNull] JobStorage storage)
        {
            if (page == null) throw new ArgumentNullException("page");
            if (storage == null) throw new ArgumentNullException("storage");

            var builder = new StringBuilder();

            var monitoringApi = storage.GetMonitoringApi();
            var statistics = monitoringApi.GetStatistics();

            Func<RazorPage, JobStorage, StatisticsDto, string>[] funcList;

            lock (ItemFuncList)
            {
                funcList = ItemFuncList.ToArray();
            }

            foreach (var func in funcList)
            {
                builder.AppendLine(func(page, storage, statistics));
            }

            return new NonEscapedString(builder.ToString());
        }
    }
}
