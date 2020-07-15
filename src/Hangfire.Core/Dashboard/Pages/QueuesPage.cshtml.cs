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
    using System;
    using System.Collections.Generic;
    
    #line 2 "..\..\Dashboard\Pages\QueuesPage.cshtml"
    using System.Linq;
    
    #line default
    #line hidden
    using System.Text;
    
    #line 3 "..\..\Dashboard\Pages\QueuesPage.cshtml"
    using Hangfire.Dashboard;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Dashboard\Pages\QueuesPage.cshtml"
    using Hangfire.Dashboard.Pages;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Dashboard\Pages\QueuesPage.cshtml"
    using Hangfire.Dashboard.Resources;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    internal partial class QueuesPage : RazorPage
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");







            
            #line 7 "..\..\Dashboard\Pages\QueuesPage.cshtml"
  
    Layout = new LayoutPage(Strings.QueuesPage_Title);

    var monitor = Storage.GetMonitoringApi();
    var queues = monitor.Queues();


            
            #line default
            #line hidden
WriteLiteral("\r\n<div class=\"row\">\r\n    <div class=\"col-md-3\">\r\n        ");


            
            #line 16 "..\..\Dashboard\Pages\QueuesPage.cshtml"
   Write(Html.JobsSidebar());

            
            #line default
            #line hidden
WriteLiteral("\r\n    </div>\r\n    <div class=\"col-md-9\">\r\n        <h1 class=\"page-header\">");


            
            #line 19 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                           Write(Strings.QueuesPage_Title);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");


            
            #line 21 "..\..\Dashboard\Pages\QueuesPage.cshtml"
         if (queues.Count == 0)
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"alert alert-warning\">\r\n                ");


            
            #line 24 "..\..\Dashboard\Pages\QueuesPage.cshtml"
           Write(Strings.QueuesPage_NoQueues);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 26 "..\..\Dashboard\Pages\QueuesPage.cshtml"
        }
        else
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"table-responsive\">\r\n                <table class=\"table t" +
"able-striped\">\r\n                    <thead>\r\n                        <tr>\r\n     " +
"                       <th style=\"min-width: 200px;\">");


            
            #line 33 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                     Write(Strings.QueuesPage_Table_Queue);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                            <th>");


            
            #line 34 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                           Write(Strings.QueuesPage_Table_Length);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                            <th>");


            
            #line 35 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                           Write(Strings.Common_Fetched);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                            <th>");


            
            #line 36 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                           Write(Strings.QueuesPage_Table_NextsJobs);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                        </tr>\r\n                    </thead>\r\n             " +
"       <tbody>\r\n");


            
            #line 40 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                         foreach (var queue in queues)
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <tr>\r\n                                <td>");


            
            #line 43 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                               Write(Html.QueueLabel(queue.Name));

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                                <td>");


            
            #line 44 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                               Write(queue.Length);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                                <td>\r\n");


            
            #line 46 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                     if (queue.Fetched.HasValue)
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <a href=\"");


            
            #line 48 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                            Write(Url.To("/jobs/enqueued/fetched/" + queue.Name));

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                            ");


            
            #line 49 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                       Write(queue.Fetched);

            
            #line default
            #line hidden
WriteLiteral("\r\n                                        </a>\r\n");


            
            #line 51 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                    }
                                    else
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <em>");


            
            #line 54 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                       Write(Strings.Common_NotAvailable);

            
            #line default
            #line hidden
WriteLiteral("</em>\r\n");


            
            #line 55 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                </td>\r\n                                <td>\r\n");


            
            #line 58 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                     if (queue.FirstJobs.Count == 0)
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <em>\r\n                                   " +
"         ");


            
            #line 61 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                       Write(Strings.QueuesPage_NoJobs);

            
            #line default
            #line hidden
WriteLiteral("\r\n                                        </em>\r\n");


            
            #line 63 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                    }
                                    else
                                    {

            
            #line default
            #line hidden
WriteLiteral(@"                                        <table class=""table table-condensed table-inner"">
                                            <thead>
                                                <tr>
                                                    <th class=""min-width"">");


            
            #line 69 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                                     Write(Strings.Common_Id);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                                    <th class=\"min-width\">" +
"");


            
            #line 70 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                                     Write(Strings.Common_State);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                                    <th>");


            
            #line 71 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                   Write(Strings.Common_Job);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                                    <th class=\"align-right" +
" min-width\">");


            
            #line 72 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                                                 Write(Strings.Common_Enqueued);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                                </tr>\r\n                   " +
"                         </thead>\r\n                                            <" +
"tbody>\r\n");


            
            #line 76 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                 foreach (var job in queue.FirstJobs)
                                                {

            
            #line default
            #line hidden
WriteLiteral("                                                    <tr class=\"");


            
            #line 78 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                           Write(job.Value == null || !job.Value.InEnqueuedState ? "obsolete-data" : null);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                                        <td class=\"min-width\"" +
">\r\n                                                            ");


            
            #line 80 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                       Write(Html.JobIdLink(job.Key));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 81 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                             if (job.Value != null && !job.Value.InEnqueuedState)
                                                            {

            
            #line default
            #line hidden
WriteLiteral("                                                                <span title=\"");


            
            #line 83 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                                        Write(Strings.Common_JobStateChanged_Text);

            
            #line default
            #line hidden
WriteLiteral("\" class=\"glyphicon glyphicon-question-sign\"></span>\r\n");


            
            #line 84 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                            }

            
            #line default
            #line hidden
WriteLiteral("                                                        </td>\r\n");


            
            #line 86 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                         if (job.Value == null)
                                                        {

            
            #line default
            #line hidden
WriteLiteral("                                                            <td colspan=\"3\"><em>");


            
            #line 88 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                                           Write(Strings.Common_JobExpired);

            
            #line default
            #line hidden
WriteLiteral("</em></td>\r\n");


            
            #line 89 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                        }
                                                        else
                                                        {

            
            #line default
            #line hidden
WriteLiteral("                                                            <td class=\"min-width\"" +
">\r\n                                                                ");


            
            #line 93 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                           Write(Html.StateLabel(job.Value.State));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                                            </td>\r\n");



WriteLiteral("                                                            <td class=\"word-break" +
"\">\r\n                                                                ");


            
            #line 96 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                           Write(Html.JobNameLink(job.Key, job.Value.Job, job.Value.InvocationData));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                                            </td>\r\n");



WriteLiteral("                                                            <td class=\"align-righ" +
"t min-width\">\r\n");


            
            #line 99 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                                 if (job.Value.EnqueuedAt.HasValue)
                                                                {
                                                                    
            
            #line default
            #line hidden
            
            #line 101 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                               Write(Html.RelativeTime(job.Value.EnqueuedAt.Value));

            
            #line default
            #line hidden
            
            #line 101 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                                                                                  
                                                                }
                                                                else
                                                                {

            
            #line default
            #line hidden
WriteLiteral("                                                                    <em>");


            
            #line 105 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                                   Write(Strings.Common_NotAvailable);

            
            #line default
            #line hidden
WriteLiteral("</em>\r\n");


            
            #line 106 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                                }

            
            #line default
            #line hidden
WriteLiteral("                                                            </td>\r\n");


            
            #line 108 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                        }

            
            #line default
            #line hidden
WriteLiteral("                                                    </tr>\r\n");


            
            #line 110 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                                }

            
            #line default
            #line hidden
WriteLiteral("                                            </tbody>\r\n                           " +
"             </table>\r\n");


            
            #line 113 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                </td>\r\n                            </tr>\r\n");


            
            #line 116 "..\..\Dashboard\Pages\QueuesPage.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </tbody>\r\n                </table>\r\n            </div>\r\n");


            
            #line 120 "..\..\Dashboard\Pages\QueuesPage.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("    </div>\r\n</div>");


        }
    }
}
#pragma warning restore 1591
