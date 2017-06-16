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
    
    #line 2 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
    using System;
    
    #line default
    #line hidden
    using System.Collections.Generic;
    
    #line 3 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
    using System.Linq;
    
    #line default
    #line hidden
    using System.Text;
    
    #line 4 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
    using Hangfire;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
    using Hangfire.Dashboard;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
    using Hangfire.Dashboard.Pages;
    
    #line default
    #line hidden
    
    #line 7 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
    using Hangfire.Dashboard.Resources;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    internal partial class ProcessingJobsPage : RazorPage
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");









            
            #line 9 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
  
    Layout = new LayoutPage(Strings.ProcessingJobsPage_Title);

    int from, perPage;

    int.TryParse(Query("from"), out from);
    int.TryParse(Query("count"), out perPage);

    var monitor = Storage.GetMonitoringApi();
    var pager = new Pager(from, perPage, monitor.ProcessingCount());
    var processingJobs = monitor.ProcessingJobs(pager.FromRecord, pager.RecordsPerPage);
    var servers = monitor.Servers();


            
            #line default
            #line hidden
WriteLiteral("\r\n<div class=\"row\">\r\n    <div class=\"col-md-3\">\r\n        ");


            
            #line 25 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
   Write(Html.JobsSidebar());

            
            #line default
            #line hidden
WriteLiteral("\r\n    </div>\r\n    <div class=\"col-md-9\">\r\n        <h1 class=\"page-header\">");


            
            #line 28 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                           Write(Strings.ProcessingJobsPage_Title);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");


            
            #line 30 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
         if (pager.TotalPageCount == 0)
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"alert alert-info\">\r\n                ");


            
            #line 33 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
           Write(Strings.ProcessingJobsPage_NoJobs);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 35 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
        }
        else
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"js-jobs-list\">\r\n                <div class=\"btn-toolbar b" +
"tn-toolbar-top\">\r\n");


            
            #line 40 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                     if (HasPermission(DashboardPermission.EnqueueJob))
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <button class=\"js-jobs-list-command btn btn-sm btn-primar" +
"y\"\r\n                                data-url=\"");


            
            #line 43 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                     Write(Url.To("/jobs/processing/requeue"));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                data-loading-text=\"");


            
            #line 44 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                              Write(Strings.Common_Enqueueing);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                disabled=\"disabled\">\r\n                        " +
"    <span class=\"glyphicon glyphicon-repeat\"></span>\r\n                          " +
"  ");


            
            #line 47 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                       Write(Strings.Common_RequeueJobs);

            
            #line default
            #line hidden
WriteLiteral("\r\n                        </button>\r\n");


            
            #line 49 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                    }

            
            #line default
            #line hidden

            
            #line 50 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                     if (HasPermission(DashboardPermission.DeleteJob))
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <button class=\"js-jobs-list-command btn btn-sm btn-defaul" +
"t\"\r\n                                data-url=\"");


            
            #line 53 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                     Write(Url.To("/jobs/processing/delete"));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                data-loading-text=\"");


            
            #line 54 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                              Write(Strings.Common_Deleting);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                data-confirm=\"");


            
            #line 55 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                         Write(Strings.Common_DeleteConfirm);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                disabled=\"disabled\">\r\n                        " +
"    <span class=\"glyphicon glyphicon-remove\"></span>\r\n                          " +
"  ");


            
            #line 58 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                       Write(Strings.Common_DeleteSelected);

            
            #line default
            #line hidden
WriteLiteral("\r\n                        </button>\r\n");


            
            #line 60 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                    ");


            
            #line 61 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
               Write(Html.PerPageSelector(pager));

            
            #line default
            #line hidden
WriteLiteral("\r\n                </div>\r\n\r\n                <div class=\"table-responsive\">\r\n     " +
"               <table class=\"table\">\r\n                        <thead>\r\n         " +
"                   <tr>\r\n");


            
            #line 68 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                 if (!IsReadOnly)
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    <th class=\"min-width\">\r\n                     " +
"                   <input type=\"checkbox\" class=\"js-jobs-list-select-all\"/>\r\n   " +
"                                 </th>\r\n");


            
            #line 73 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                }

            
            #line default
            #line hidden
WriteLiteral("                                <th class=\"min-width\">");


            
            #line 74 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                 Write(Strings.Common_Id);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"min-width\">");


            
            #line 75 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                 Write(Strings.Common_Server);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th>");


            
            #line 76 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                               Write(Strings.Common_Job);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"align-right\">");


            
            #line 77 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                   Write(Strings.ProcessingJobsPage_Table_Started);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                            </tr>\r\n                        </thead>\r\n     " +
"                   <tbody>\r\n");


            
            #line 81 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                             foreach (var job in processingJobs)
                            {

            
            #line default
            #line hidden
WriteLiteral("                                <tr class=\"js-jobs-list-row ");


            
            #line 83 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                        Write(!job.Value.InProcessingState ? "obsolete-data" : null);

            
            #line default
            #line hidden
WriteLiteral(" ");


            
            #line 83 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                                                                                 Write(job.Value.InProcessingState ? "hover" : null);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n");


            
            #line 84 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                     if (!IsReadOnly)
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <td>\r\n");


            
            #line 87 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                             if (job.Value.InProcessingState)
                                            {

            
            #line default
            #line hidden
WriteLiteral("                                                <input type=\"checkbox\" class=\"js-" +
"jobs-list-checkbox\" name=\"jobs[]\" value=\"");


            
            #line 89 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                                                                                     Write(job.Key);

            
            #line default
            #line hidden
WriteLiteral("\"/>\r\n");


            
            #line 90 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                            }

            
            #line default
            #line hidden
WriteLiteral("                                        </td>\r\n");


            
            #line 92 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                    <td class=\"min-width\">\r\n                     " +
"                   ");


            
            #line 94 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                   Write(Html.JobIdLink(job.Key));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 95 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                         if (!job.Value.InProcessingState)
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <span title=\"");


            
            #line 97 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                    Write(Strings.Common_JobStateChanged_Text);

            
            #line default
            #line hidden
WriteLiteral("\" class=\"glyphicon glyphicon-question-sign\"></span>\r\n");


            
            #line 98 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                        }

            
            #line default
            #line hidden
WriteLiteral("                                    </td>\r\n");


            
            #line 100 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                     if (!job.Value.InProcessingState)
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <td colspan=\"3\">");


            
            #line 102 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                   Write(Strings.Common_JobStateChanged_Text);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n");


            
            #line 103 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                    }
                                    else
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <td class=\"min-width\">\r\n                 " +
"                           ");


            
            #line 107 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                       Write(Html.ServerId(job.Value.ServerId));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                        </td>\r\n");



WriteLiteral("                                        <td class=\"word-break\">\r\n");


            
            #line 110 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                             if (servers.All(x => x.Name != job.Value.ServerId || x.Heartbeat < DateTime.UtcNow.AddMinutes(-1)))
                                            {

            
            #line default
            #line hidden
WriteLiteral("                                                <span title=\"");


            
            #line 112 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                        Write(Strings.ProcessingJobsPage_Aborted);

            
            #line default
            #line hidden
WriteLiteral("\" class=\"glyphicon glyphicon-warning-sign\" style=\"font-size: 10px;\"></span>\r\n");


            
            #line 113 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                            }

            
            #line default
            #line hidden
WriteLiteral("\r\n                                            ");


            
            #line 115 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                       Write(Html.JobNameLink(job.Key, job.Value.Job));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                        </td>\r\n");



WriteLiteral("                                        <td class=\"align-right\">\r\n");


            
            #line 118 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                             if (job.Value.StartedAt.HasValue)
                                            {
                                                
            
            #line default
            #line hidden
            
            #line 120 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                           Write(Html.RelativeTime(job.Value.StartedAt.Value));

            
            #line default
            #line hidden
            
            #line 120 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                                                             
                                            }

            
            #line default
            #line hidden
WriteLiteral("                                        </td>\r\n");


            
            #line 123 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                </tr>\r\n");


            
            #line 125 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                            }

            
            #line default
            #line hidden
WriteLiteral("                        </tbody>\r\n                    </table>\r\n                <" +
"/div>\r\n\r\n                ");


            
            #line 130 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
           Write(Html.Paginator(pager));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 132 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("    </div>\r\n</div>");


        }
    }
}
#pragma warning restore 1591
