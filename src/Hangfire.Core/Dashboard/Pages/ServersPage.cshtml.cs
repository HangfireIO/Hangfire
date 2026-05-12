#pragma warning disable 1591

namespace Hangfire.Dashboard.Pages
{
    using System;
    using System.Linq;
    using Hangfire.Common;
    using Hangfire.Dashboard;
    using Hangfire.Dashboard.Resources;

    internal partial class ServersPage : RazorPage
    {
        public override void Execute()
        {
            Layout = new LayoutPage(Strings.ServersPage_Title);

            var monitor = Storage.GetMonitoringApi();
            var servers = monitor.Servers();
            var now = StorageUtcNow ?? ApplicationUtcNow;
            var inconclusiveThreshold = DashboardOptions.ServerPossiblyAbortedThreshold;
            var possiblyAbortedThreshold = TimeSpan.FromSeconds(inconclusiveThreshold.TotalSeconds * 2);

            WriteLiteral("\r\n<div class=\"row\">\r\n    <div class=\"col-md-12\">\r\n        <h1 id=\"page-title\" class=\"page-header\">");
            Write(Strings.ServersPage_Title);
            WriteLiteral("</h1>\r\n\r\n");

            if (servers.Count == 0)
            {
                WriteLiteral("            <div class=\"alert alert-warning\">\r\n                ");
                Write(Strings.ServersPage_NoServers);
                WriteLiteral("\r\n            </div>\r\n");
            }
            else
            {
                if (servers.Any(x => x.Heartbeat.HasValue && x.Heartbeat.Value < now.Add(-possiblyAbortedThreshold)))
                {
                    WriteLiteral("                <div class=\"alert alert-info\">\r\n                    <h4>");
                    Write(Strings.ServersPage_Note_Title);
                    WriteLiteral("</h4>\r\n                    ");
                    Write(Html.Raw(string.Format(Strings.ServersPage_Note_Text, Url.To("/jobs/processing"))));
                    WriteLiteral("\r\n                </div>\r\n");
                }

                WriteLiteral("            <div class=\"table-responsive\">\r\n                <table class=\"table\" aria-describedby=\"page-title\">\r\n                    <thead>\r\n                        <tr>\r\n                            <th>");
                Write(Strings.ServersPage_Table_Name);
                WriteLiteral("</th>\r\n                            <th>");
                Write(Strings.ServersPage_Table_Workers);
                WriteLiteral("</th>\r\n                            <th>");
                Write(Strings.ServersPage_Table_Queues);
                WriteLiteral("</th>\r\n                            <th>Allocation</th>\r\n                            <th>Reason</th>\r\n                            <th>Checked</th>\r\n                            <th>");
                Write(Strings.ServersPage_Table_Started);
                WriteLiteral("</th>\r\n                            <th>");
                Write(Strings.ServersPage_Table_Heartbeat);
                WriteLiteral("</th>\r\n                        </tr>\r\n                    </thead>\r\n                    <tbody>\r\n");

                foreach (var server in servers)
                {
                    var isOffline = server.Heartbeat < now.Add(-possiblyAbortedThreshold);
                    var allocationState = isOffline
                        ? JobServerAllocationState.Offline
                        : (server.AllocationState ?? (server.CanAllocate ? JobServerAllocationState.Available : JobServerAllocationState.ResourceConstrained));
                    var allocationLabel = allocationState == JobServerAllocationState.Available
                        ? "label-success"
                        : (allocationState == JobServerAllocationState.Offline ? "label-danger" : "label-warning");
                    var queueLabels = server.Queues.Select(queue =>
                    {
                        JobServerQueueResourceSnapshot queueAllocation;
                        if (server.QueueAllocation != null &&
                            server.QueueAllocation.TryGetValue(queue, out queueAllocation) &&
                            !queueAllocation.CanAllocate)
                        {
                            var title = String.IsNullOrWhiteSpace(queueAllocation.Reason) ? "Paused by resource policy" : queueAllocation.Reason;
                            return Html.QueueLabel(queue) + " <span class=\"label label-warning\" title=\"" + Html.HtmlEncode(title) + "\">paused</span>";
                        }

                        return Html.QueueLabel(queue).ToString();
                    });

                    WriteLiteral("                            <tr>\r\n                                <td>\r\n");
                    if (isOffline)
                    {
                        WriteLiteral("                                        <span class=\"glyphicon glyphicon-alert text-danger\" title=\"");
                        Write(Strings.ServersPage_Possibly_Aborted);
                        WriteLiteral("\"></span>&nbsp;");
                        Write(Html.ServerId(server.Name));
                        WriteLiteral("\r\n");
                    }
                    else if (server.Heartbeat < now.Add(-inconclusiveThreshold))
                    {
                        WriteLiteral("                                        <span class=\"glyphicon margin-right-14p\"></span>&nbsp;");
                        Write(Html.ServerId(server.Name));
                        WriteLiteral("\r\n");
                    }
                    else
                    {
                        WriteLiteral("                                        <span class=\"glyphicon glyphicon-ok text-success\" title=\"");
                        Write(Strings.ServersPage_Active);
                        WriteLiteral("\"></span>&nbsp;");
                        Write(Html.ServerId(server.Name));
                        WriteLiteral("\r\n");
                    }

                    WriteLiteral("                                </td>\r\n                                <td>");
                    Write(server.WorkersCount);
                    WriteLiteral("</td>\r\n                                <td>");
                    Write(Html.Raw(String.Join(", ", queueLabels)));
                    WriteLiteral("</td>\r\n                                <td><span class=\"label ");
                    Write(allocationLabel);
                    WriteLiteral("\">");
                    Write(allocationState);
                    WriteLiteral("</span></td>\r\n                                <td>");
                    Write(server.AllocationReason);
                    WriteLiteral("</td>\r\n                                <td>");
                    if (server.AllocationCheckedAt.HasValue)
                    {
                        Write(Html.RelativeTime(server.AllocationCheckedAt.Value));
                    }

                    WriteLiteral("</td>\r\n                                <td>");
                    Write(Html.RelativeTime(server.StartedAt));
                    WriteLiteral("</td>\r\n                                <td>\r\n");
                    if (server.Heartbeat.HasValue)
                    {
                        Write(Html.RelativeTime(server.Heartbeat.Value));
                    }

                    WriteLiteral("                                </td>\r\n                            </tr>\r\n");
                }

                WriteLiteral("                    </tbody>\r\n                </table>\r\n            </div>\r\n");
            }

            WriteLiteral("    </div>\r\n</div>");
        }
    }
}

#pragma warning restore 1591
