﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Hangfire.Dashboard.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    #line 2 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
    using Hangfire.Dashboard;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
    using Hangfire.Dashboard.Pages;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
    using Hangfire.Dashboard.Resources;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    internal partial class ScheduledJobsPage : RazorPage
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");






            
            #line 6 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
  
    Layout = new LayoutPage(Strings.ScheduledJobsPage_Title);

    int from, perPage;

    int.TryParse(Query("from"), out from);
    int.TryParse(Query("count"), out perPage);

    var monitor = Storage.GetMonitoringApi();
    var pager = new Pager(from, perPage, monitor.ScheduledCount());
    var scheduledJobs = monitor.ScheduledJobs(pager.FromRecord, pager.RecordsPerPage);


            
            #line default
            #line hidden
WriteLiteral("\r\n<div class=\"row\">\r\n    <div class=\"col-md-3\">\r\n        ");


            
            #line 21 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
   Write(Html.JobsSidebar());

            
            #line default
            #line hidden
WriteLiteral("\r\n    </div>\r\n    <div class=\"col-md-9\">\r\n        <h1 class=\"page-header\">");


            
            #line 24 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                           Write(Strings.ScheduledJobsPage_Title);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");


            
            #line 26 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
         if (pager.TotalPageCount == 0)
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"alert alert-info\">\r\n                ");


            
            #line 29 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
           Write(Strings.ScheduledJobsPage_NoJobs);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 31 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
        }
        else
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"js-jobs-list\">\r\n                <div class=\"btn-toolbar b" +
"tn-toolbar-top\">\r\n                    <button class=\"js-jobs-list-command btn bt" +
"n-sm btn-primary\"\r\n                            data-url=\"");


            
            #line 37 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                 Write(Url.To("/jobs/scheduled/enqueue"));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            data-loading-text=\"");


            
            #line 38 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                          Write(Strings.Common_Enqueueing);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            disabled=\"disabled\">\r\n                        <spa" +
"n class=\"glyphicon glyphicon-play\"></span>\r\n                        ");


            
            #line 41 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                   Write(Strings.ScheduledJobsPage_EnqueueNow);

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </button>\r\n\r\n                    <button class=\"js-jobs-lis" +
"t-command btn btn-sm btn-default\"\r\n                            data-url=\"");


            
            #line 45 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                 Write(Url.To("/jobs/scheduled/delete"));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            data-loading-text=\"");


            
            #line 46 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                          Write(Strings.Common_Deleting);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            data-confirm=\"");


            
            #line 47 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                     Write(Strings.Common_DeleteConfirm);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            disabled=\"disabled\">\r\n                        <spa" +
"n class=\"glyphicon glyphicon-remove\"></span>\r\n                        ");


            
            #line 50 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                   Write(Strings.Common_DeleteSelected);

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </button>\r\n\r\n                    ");


            
            #line 53 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
               Write(Html.PerPageSelector(pager));

            
            #line default
            #line hidden
WriteLiteral(@"
                </div>

                <div class=""table-responsive"">
                    <table class=""table"">
                        <thead>
                            <tr>
                                <th class=""min-width"">
                                    <input type=""checkbox"" class=""js-jobs-list-select-all"" />
                                </th>
                                <th class=""min-width"">");


            
            #line 63 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                                 Write(Strings.Common_Id);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th>");


            
            #line 64 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                               Write(Strings.ScheduledJobsPage_Table_Enqueue);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th>");


            
            #line 65 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                               Write(Strings.Common_Job);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"align-right\">ScheduledJobsPage_" +
"Table_Scheduled</th>\r\n                            </tr>\r\n                       " +
" </thead>\r\n");


            
            #line 69 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                         foreach (var job in scheduledJobs)
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <tr class=\"js-jobs-list-row ");


            
            #line 71 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                                    Write(!job.Value.InScheduledState ? "obsolete-data" : null);

            
            #line default
            #line hidden
WriteLiteral(" ");


            
            #line 71 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                                                                                            Write(job.Value.InScheduledState ? "hover" : null);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                <td>\r\n");


            
            #line 73 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                     if (job.Value.InScheduledState)
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <input type=\"checkbox\" class=\"js-jobs-lis" +
"t-checkbox\" name=\"jobs[]\" value=\"");


            
            #line 75 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                                                                                             Write(job.Key);

            
            #line default
            #line hidden
WriteLiteral("\" />\r\n");


            
            #line 76 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                </td>\r\n                                <td class=" +
"\"min-width\">\r\n                                    ");


            
            #line 79 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                               Write(Html.JobIdLink(job.Key));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 80 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                     if (!job.Value.InScheduledState)
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <span title=\"");


            
            #line 82 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                                Write(Strings.Common_JobStateChanged_Text);

            
            #line default
            #line hidden
WriteLiteral("\" class=\"glyphicon glyphicon-question-sign\"></span>\r\n");


            
            #line 83 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                </td>\r\n                                <td class=" +
"\"min-width\">\r\n                                    ");


            
            #line 86 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                               Write(Html.RelativeTime(job.Value.EnqueueAt));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                </td>\r\n                                <td clas" +
"s=\"word-break\">\r\n                                    ");


            
            #line 89 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                               Write(Html.JobNameLink(job.Key, job.Value.Job));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                </td>\r\n                                <td clas" +
"s=\"align-right\">\r\n");


            
            #line 92 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                     if (job.Value.ScheduledAt.HasValue)
                                    {
                                        
            
            #line default
            #line hidden
            
            #line 94 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                   Write(Html.RelativeTime(job.Value.ScheduledAt.Value));

            
            #line default
            #line hidden
            
            #line 94 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                                                                                       
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                </td>\r\n                            </tr>\r\n");


            
            #line 98 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </table>\r\n                </div>\r\n\r\n                ");


            
            #line 102 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
           Write(Html.Paginator(pager));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 104 "..\..\Dashboard\Pages\ScheduledJobsPage.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("    </div>\r\n</div>");


        }
    }
}
#pragma warning restore 1591
