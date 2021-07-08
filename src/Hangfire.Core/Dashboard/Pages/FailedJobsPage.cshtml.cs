#pragma warning disable 1591
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
    
    #line 2 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
    using System;
    
    #line default
    #line hidden
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    #line 3 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
    using Hangfire;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
    using Hangfire.Dashboard;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
    using Hangfire.Dashboard.Pages;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
    using Hangfire.Dashboard.Resources;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    internal partial class FailedJobsPage : RazorPage
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");








            
            #line 8 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
  
    Layout = new LayoutPage(Strings.FailedJobsPage_Title);

    int from, perPage;

    int.TryParse(Query("from"), out from);
    int.TryParse(Query("count"), out perPage);

    var monitor = Storage.GetMonitoringApi();
    var pager = new Pager(from, perPage, monitor.FailedCount());
    var failedJobs = monitor.FailedJobs(pager.FromRecord, pager.RecordsPerPage);


            
            #line default
            #line hidden
WriteLiteral("\r\n<div class=\"row\">\r\n    <div class=\"col-md-3\">\r\n        ");


            
            #line 23 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
   Write(Html.JobsSidebar());

            
            #line default
            #line hidden
WriteLiteral("\r\n    </div>\r\n    <div class=\"col-md-9\">\r\n        <h1 class=\"page-header\">");


            
            #line 26 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                           Write(Strings.FailedJobsPage_Title);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");


            
            #line 28 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
         if (pager.TotalPageCount == 0)
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"alert alert-success\">\r\n               ");


            
            #line 31 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
          Write(Strings.FailedJobsPage_NoJobs);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 33 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
        }
        else
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"alert alert-warning\">\r\n                ");


            
            #line 37 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
           Write(Html.Raw(Strings.FailedJobsPage_FailedJobsNotExpire_Warning_Html));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 39 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
            

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"js-jobs-list\">\r\n                <div class=\"btn-toolbar b" +
"tn-toolbar-top\">\r\n");


            
            #line 42 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                     if (!IsReadOnly)
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <button class=\"js-jobs-list-command btn btn-sm btn-primar" +
"y\"\r\n                                data-url=\"");


            
            #line 45 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                     Write(Url.To("/jobs/failed/requeue"));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                data-loading-text=\"");


            
            #line 46 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                              Write(Strings.Common_Enqueueing);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                disabled=\"disabled\">\r\n                        " +
"    <span class=\"glyphicon glyphicon-repeat\"></span>\r\n                          " +
"  ");


            
            #line 49 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                       Write(Strings.Common_RequeueJobs);

            
            #line default
            #line hidden
WriteLiteral("\r\n                        </button>\r\n");


            
            #line 51 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                    }

            
            #line default
            #line hidden

            
            #line 52 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                     if (!IsReadOnly)
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <button class=\"js-jobs-list-command btn btn-sm btn-defaul" +
"t\"\r\n                                data-url=\"");


            
            #line 55 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                     Write(Url.To("/jobs/failed/delete"));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                data-loading-text=\"");


            
            #line 56 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                              Write(Strings.Common_Deleting);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                data-confirm=\"");


            
            #line 57 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                         Write(Strings.Common_DeleteConfirm);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                            <span class=\"glyphicon glyphicon-remove\"></span>\r" +
"\n                            ");


            
            #line 59 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                       Write(Strings.Common_DeleteSelected);

            
            #line default
            #line hidden
WriteLiteral("\r\n                        </button>\r\n");


            
            #line 61 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                    ");


            
            #line 62 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
               Write(Html.PerPageSelector(pager));

            
            #line default
            #line hidden
WriteLiteral("\r\n                </div>\r\n\r\n                <div class=\"table-responsive\">\r\n     " +
"               <table class=\"table\">\r\n                        <thead>\r\n         " +
"                   <tr>\r\n");


            
            #line 69 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                 if (!IsReadOnly)
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    <th class=\"min-width\">\r\n                     " +
"                   <input type=\"checkbox\" class=\"js-jobs-list-select-all\"/>\r\n   " +
"                                 </th>\r\n");


            
            #line 74 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                }

            
            #line default
            #line hidden
WriteLiteral("                                <th class=\"min-width\">");


            
            #line 75 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                 Write(Strings.Common_Id);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th>");


            
            #line 76 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                               Write(Strings.Common_Job);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"align-right\">");


            
            #line 77 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                   Write(Strings.FailedJobsPage_Table_Failed);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                            </tr>\r\n                        </thead>\r\n     " +
"                   <tbody>\r\n");


            
            #line 81 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                               var index = 0; 

            
            #line default
            #line hidden

            
            #line 82 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                             foreach (var job in failedJobs)
                            {

            
            #line default
            #line hidden
WriteLiteral("                                <tr class=\"js-jobs-list-row ");


            
            #line 84 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                        Write(job.Value == null || !job.Value.InFailedState ? "obsolete-data" : null);

            
            #line default
            #line hidden
WriteLiteral(" ");


            
            #line 84 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                                                                                                  Write(job.Value != null && job.Value.InFailedState ? "hover" : null);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n");


            
            #line 85 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                     if (!IsReadOnly)
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <td rowspan=\"");


            
            #line 87 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                 Write(job.Value != null && job.Value.InFailedState ? "2" : "1");

            
            #line default
            #line hidden
WriteLiteral("\">\r\n");


            
            #line 88 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                             if (job.Value != null && job.Value.InFailedState)
                                            {

            
            #line default
            #line hidden
WriteLiteral("                                                <input type=\"checkbox\" class=\"js-" +
"jobs-list-checkbox\" name=\"jobs[]\" value=\"");


            
            #line 90 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                                                                                     Write(job.Key);

            
            #line default
            #line hidden
WriteLiteral("\" />\r\n");


            
            #line 91 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                            }

            
            #line default
            #line hidden
WriteLiteral("                                        </td>\r\n");


            
            #line 93 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                    <td class=\"min-width\" rowspan=\"");


            
            #line 94 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                               Write(job.Value != null && job.Value.InFailedState ? "2" : "1");

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                        ");


            
            #line 95 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                   Write(Html.JobIdLink(job.Key));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 96 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                         if (job.Value != null && !job.Value.InFailedState)
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <span title=\"");


            
            #line 98 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                    Write(Strings.Common_JobStateChanged_Text);

            
            #line default
            #line hidden
WriteLiteral("\" class=\"glyphicon glyphicon-question-sign\"></span>\r\n");


            
            #line 99 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                        }

            
            #line default
            #line hidden
WriteLiteral("                                    </td>\r\n");


            
            #line 101 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                     if (job.Value == null)
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <td colspan=\"2\">\r\n                       " +
"                     <em>");


            
            #line 104 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                           Write(Strings.Common_JobExpired);

            
            #line default
            #line hidden
WriteLiteral("</em>\r\n                                        </td>\r\n");


            
            #line 106 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                    }
                                    else
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <td>\r\n                                   " +
"         <div class=\"word-break\">\r\n                                             " +
"   ");


            
            #line 111 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                           Write(Html.JobNameLink(job.Key, job.Value.Job));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                            </div>\r\n");


            
            #line 113 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                             if (!String.IsNullOrEmpty(job.Value.ExceptionMessage))
                                            {

            
            #line default
            #line hidden
WriteLiteral("                                                <div style=\"color: #888;\">\r\n     " +
"                                               ");


            
            #line 116 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                               Write(job.Value.Reason);

            
            #line default
            #line hidden
WriteLiteral(" <a class=\"expander\" href=\"#\">");


            
            #line 116 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                                                               Write(index == 0 ? Strings.Common_LessDetails : Strings.Common_MoreDetails);

            
            #line default
            #line hidden
WriteLiteral("</a>\r\n                                                </div>\r\n");


            
            #line 118 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                            }

            
            #line default
            #line hidden
WriteLiteral("                                        </td>\r\n");



WriteLiteral("                                        <td class=\"align-right\">\r\n");


            
            #line 121 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                             if (job.Value.FailedAt.HasValue)
                                            {
                                                
            
            #line default
            #line hidden
            
            #line 123 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                           Write(Html.RelativeTime(job.Value.FailedAt.Value));

            
            #line default
            #line hidden
            
            #line 123 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                                                            
                                            }

            
            #line default
            #line hidden
WriteLiteral("                                        </td>\r\n");


            
            #line 126 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                </tr>\r\n");


            
            #line 128 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                if (job.Value != null && job.Value.InFailedState)
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    <tr>\r\n                                       " +
" <td colspan=\"2\" class=\"failed-job-details\">\r\n                                  " +
"          <div class=\"expandable\" style=\"");


            
            #line 132 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                                       Write(index++ == 0 ? "display: block;" : null);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n");


            
            #line 133 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                  
                                                    var serverId = job.Value.StateData != null && job.Value.StateData.ContainsKey("ServerId") ? $" ({Html.ServerId(job.Value.StateData["ServerId"])})" : null;
                                                

            
            #line default
            #line hidden
WriteLiteral("                                                <h4>");


            
            #line 136 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                               Write(job.Value.ExceptionType);

            
            #line default
            #line hidden

            
            #line 136 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                                       Write(Html.Raw(serverId));

            
            #line default
            #line hidden
WriteLiteral("</h4>\r\n                                                <p class=\"text-muted\">\r\n  " +
"                                                  ");


            
            #line 138 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                               Write(job.Value.ExceptionMessage);

            
            #line default
            #line hidden
WriteLiteral("\r\n                                                </p>\r\n\r\n");


            
            #line 141 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                 if (!String.IsNullOrEmpty(job.Value.ExceptionDetails))
                                                {

            
            #line default
            #line hidden
WriteLiteral("                                                    <pre class=\"stack-trace\"><cod" +
"e>");


            
            #line 143 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                                              Write(Html.StackTrace(job.Value.ExceptionDetails));

            
            #line default
            #line hidden
WriteLiteral("</code></pre>\r\n");


            
            #line 144 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                                }

            
            #line default
            #line hidden
WriteLiteral("                                            </div>\r\n                             " +
"           </td>\r\n                                    </tr>\r\n");


            
            #line 148 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
                                }
                            }

            
            #line default
            #line hidden
WriteLiteral("                        </tbody>\r\n                    </table>\r\n                <" +
"/div>\r\n\r\n                ");


            
            #line 154 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
           Write(Html.Paginator(pager));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 156 "..\..\Dashboard\Pages\FailedJobsPage.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("    </div>\r\n</div>");


        }
    }
}
#pragma warning restore 1591
