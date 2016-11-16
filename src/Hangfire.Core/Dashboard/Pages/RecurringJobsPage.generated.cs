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
    
    #line 2 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
    using System;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
    using System.Collections.Generic;
    
    #line default
    #line hidden
    using System.Linq;
    using System.Text;
    
    #line 4 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
    using Hangfire.Dashboard;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
    using Hangfire.Dashboard.Pages;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
    using Hangfire.Dashboard.Resources;
    
    #line default
    #line hidden
    
    #line 7 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
    using Hangfire.Storage;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    internal partial class RecurringJobsPage : RazorPage
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");









            
            #line 9 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
  
    Layout = new LayoutPage(Strings.RecurringJobsPage_Title);
	List<RecurringJobDto> recurringJobs;
    
    int from, perPage;

    int.TryParse(Query("from"), out from);
    int.TryParse(Query("count"), out perPage);

    Pager pager = null;

	using (var connection = Storage.GetConnection())
	{
	    var storageConnection = connection as JobStorageConnection;
	    if (storageConnection != null)
	    {
	        pager = new Pager(from, perPage, storageConnection.GetRecurringJobCount());
	        recurringJobs = storageConnection.GetRecurringJobs(pager.FromRecord, pager.FromRecord + pager.RecordsPerPage - 1);
	    }
	    else
	    {
            recurringJobs = connection.GetRecurringJobs();
	    }
	}


            
            #line default
            #line hidden
WriteLiteral("\r\n<div class=\"row\">\r\n    <div class=\"col-md-12\">\r\n        <h1 class=\"page-header\"" +
">");


            
            #line 37 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                           Write(Strings.RecurringJobsPage_Title);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");


            
            #line 39 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
         if (recurringJobs.Count == 0)
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"alert alert-info\">\r\n                ");


            
            #line 42 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
           Write(Strings.RecurringJobsPage_NoJobs);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 44 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
        }
        else
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"js-jobs-list\">\r\n                <div class=\"btn-toolbar b" +
"tn-toolbar-top\">\r\n                    <button class=\"js-jobs-list-command btn bt" +
"n-sm btn-primary\"\r\n                            data-url=\"");


            
            #line 50 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                 Write(Url.To("/recurring/trigger"));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            data-loading-text=\"");


            
            #line 51 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                          Write(Strings.RecurringJobsPage_Triggering);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            disabled=\"disabled\">\r\n                        <spa" +
"n class=\"glyphicon glyphicon-play-circle\"></span>\r\n                        ");


            
            #line 54 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                   Write(Strings.RecurringJobsPage_TriggerNow);

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </button>\r\n\r\n                    <button class=\"js-jobs-lis" +
"t-command btn btn-sm btn-default\"\r\n                            data-url=\"");


            
            #line 58 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                 Write(Url.To("/recurring/remove"));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            data-loading-text=\"");


            
            #line 59 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                          Write(Strings.Common_Deleting);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            data-confirm=\"");


            
            #line 60 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                     Write(Strings.Common_DeleteConfirm);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            disabled=\"disabled\">\r\n                        <spa" +
"n class=\"glyphicon glyphicon-remove\"></span>\r\n                        ");


            
            #line 63 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                   Write(Strings.Common_Delete);

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </button>\r\n\r\n");


            
            #line 66 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                     if (pager != null)
                    {

            
            #line default
            #line hidden
WriteLiteral("                        ");

WriteLiteral(" ");


            
            #line 68 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                      Write(Html.PerPageSelector(pager));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 69 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral(@"                </div>

                <div class=""table-responsive"">
                    <table class=""table"">
                        <thead>
                            <tr>
                                <th class=""min-width"">
                                    <input type=""checkbox"" class=""js-jobs-list-select-all"" />
                                </th>
                                <th class=""min-width"">");


            
            #line 79 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                 Write(Strings.Common_Id);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"min-width\">");


            
            #line 80 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                 Write(Strings.RecurringJobsPage_Table_Cron);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"min-width\">");


            
            #line 81 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                 Write(Strings.RecurringJobsPage_Table_TimeZone);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th>");


            
            #line 82 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                               Write(Strings.Common_Job);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"align-right min-width\">");


            
            #line 83 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                             Write(Strings.RecurringJobsPage_Table_NextExecution);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"align-right min-width\">");


            
            #line 84 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                             Write(Strings.RecurringJobsPage_Table_LastExecution);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"align-right min-width\">");


            
            #line 85 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                             Write(Strings.Common_Created);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                            </tr>\r\n                        </thead>\r\n     " +
"                   <tbody>\r\n");


            
            #line 89 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                             foreach (var job in recurringJobs)
                            {

            
            #line default
            #line hidden
WriteLiteral("                                <tr class=\"js-jobs-list-row hover\">\r\n            " +
"                        <td>\r\n                                        <input typ" +
"e=\"checkbox\" class=\"js-jobs-list-checkbox\" name=\"jobs[]\" value=\"");


            
            #line 93 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                                                                             Write(job.Id);

            
            #line default
            #line hidden
WriteLiteral("\" />\r\n                                    </td>\r\n                                " +
"    <td class=\"min-width\">");


            
            #line 95 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                     Write(job.Id);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                                    <td class=\"min-width\">\r\n              " +
"                          ");



WriteLiteral("\r\n");


            
            #line 98 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                          
                                            string cronDescription = null;
#if NETFULL
                                            try
                                            {
                                                cronDescription = CronExpressionDescriptor.ExpressionDescriptor.GetDescription(job.Cron);
                                            }
                                            catch (Exception ex) when (ex is FormatException || ex is MissingFieldException)
                                            {
                                            }
#endif
                                        

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 111 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                         if (cronDescription != null)
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <code title=\"");


            
            #line 113 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                    Write(cronDescription);

            
            #line default
            #line hidden
WriteLiteral("\">");


            
            #line 113 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                                      Write(job.Cron);

            
            #line default
            #line hidden
WriteLiteral("</code>\r\n");


            
            #line 114 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                        }
                                        else
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <code>");


            
            #line 117 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                             Write(job.Cron);

            
            #line default
            #line hidden
WriteLiteral("</code>\r\n");


            
            #line 118 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                        }

            
            #line default
            #line hidden
WriteLiteral("                                    </td>\r\n                                    <t" +
"d class=\"min-width\">\r\n");


            
            #line 121 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                         if (!String.IsNullOrWhiteSpace(job.TimeZoneId))
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <span title=\"");


            
            #line 123 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                    Write(TimeZoneInfo.FindSystemTimeZoneById(job.TimeZoneId).DisplayName);

            
            #line default
            #line hidden
WriteLiteral("\" data-container=\"body\">");


            
            #line 123 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                                                                                                            Write(job.TimeZoneId);

            
            #line default
            #line hidden
WriteLiteral("</span>\r\n");


            
            #line 124 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                        }
                                        else
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            ");

WriteLiteral(" UTC\r\n");


            
            #line 128 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                    </td>\r\n                                    <t" +
"d class=\"word-break\">\r\n");


            
            #line 131 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                         if (job.Job != null)
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            ");

WriteLiteral(" ");


            
            #line 133 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                          Write(Html.JobName(job.Job));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 134 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                        }
                                        else
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <em>");


            
            #line 137 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                           Write(job.LoadException.InnerException.Message);

            
            #line default
            #line hidden
WriteLiteral("</em>\r\n");


            
            #line 138 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                        }

            
            #line default
            #line hidden
WriteLiteral("                                    </td>\r\n                                    <t" +
"d class=\"align-right min-width\">\r\n");


            
            #line 141 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                         if (job.NextExecution != null)
                                        {
                                            
            
            #line default
            #line hidden
            
            #line 143 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                       Write(Html.RelativeTime(job.NextExecution.Value));

            
            #line default
            #line hidden
            
            #line 143 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                                                       
                                        }
                                        else
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <em>");


            
            #line 147 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                           Write(Strings.Common_NotAvailable);

            
            #line default
            #line hidden
WriteLiteral("</em>\r\n");


            
            #line 148 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                        }

            
            #line default
            #line hidden
WriteLiteral("                                    </td>\r\n                                    <t" +
"d class=\"align-right min-width\">\r\n");


            
            #line 151 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                         if (job.LastExecution != null)
                                        {
                                            if (!String.IsNullOrEmpty(job.LastJobId))
                                            {

            
            #line default
            #line hidden
WriteLiteral("                                                <a href=\"");


            
            #line 155 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                    Write(Url.JobDetails(job.LastJobId));

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                                    <span class=\"label label-" +
"default label-hover\" style=\"");


            
            #line 156 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                                                                     Write($"background-color: {JobHistoryRenderer.GetForegroundStateColor(job.LastJobState)};");

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                                        ");


            
            #line 157 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                   Write(Html.RelativeTime(job.LastExecution.Value));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                                    </span>\r\n                  " +
"                              </a>\r\n");


            
            #line 160 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                            }
                                            else
                                            {

            
            #line default
            #line hidden
WriteLiteral("                                                <em>\r\n                           " +
"                         ");


            
            #line 164 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                               Write(Strings.RecurringJobsPage_Canceled);

            
            #line default
            #line hidden
WriteLiteral(" ");


            
            #line 164 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                                                   Write(Html.RelativeTime(job.LastExecution.Value));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                                </em>\r\n");


            
            #line 166 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                            }
                                        }
                                        else
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <em>");


            
            #line 170 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                           Write(Strings.Common_NotAvailable);

            
            #line default
            #line hidden
WriteLiteral("</em>\r\n");


            
            #line 171 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                        }

            
            #line default
            #line hidden
WriteLiteral("                                    </td>\r\n                                    <t" +
"d class=\"align-right min-width\">\r\n");


            
            #line 174 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                         if (job.CreatedAt != null)
                                        {
                                            
            
            #line default
            #line hidden
            
            #line 176 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                       Write(Html.RelativeTime(job.CreatedAt.Value));

            
            #line default
            #line hidden
            
            #line 176 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                                                                   
                                        }
                                        else
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <em>N/A</em>\r\n");


            
            #line 181 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                                        }

            
            #line default
            #line hidden
WriteLiteral("                                    </td>\r\n                                </tr>\r" +
"\n");


            
            #line 184 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                             }

            
            #line default
            #line hidden
WriteLiteral("                        </tbody>\r\n                    </table>\r\n                <" +
"/div>\r\n\r\n");


            
            #line 189 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                 if (pager != null)
                {

            
            #line default
            #line hidden
WriteLiteral("                    ");

WriteLiteral(" ");


            
            #line 191 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                  Write(Html.Paginator(pager));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 192 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("            </div>\r\n");


            
            #line 194 "..\..\Dashboard\Pages\RecurringJobsPage.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("    </div>\r\n</div>    ");


        }
    }
}
#pragma warning restore 1591
