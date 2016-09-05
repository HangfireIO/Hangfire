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
    
    #line 3 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
    using System.Collections.Generic;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
    using System.Linq;
    
    #line default
    #line hidden
    using System.Text;
    
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
    
    #line 8 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
    using Hangfire.States;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    internal partial class ProcessingJobsPage : RazorPage
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");










            
            #line 10 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
  
    Layout = new LayoutPage(Strings.ProcessingJobsPage_Title);

    int from, perPage;

    int.TryParse(Query("from"), out from);
    int.TryParse(Query("count"), out perPage);
    string filterString = Query("filterString");
    string filterMethodString = Query("filterMethodString");
    string startDate = Query("startDate");
    string endDate = Query("endDate");
    string startTime = Query("startTime");
    string endTime = Query("endTime");

    var monitor = Storage.GetMonitoringApi();
    var countParameters = new Dictionary<string, string>()
    {
        { "stateName", ProcessingState.StateName },
        { "filterString", filterString },
        { "filterMethodString", filterMethodString },
        { "startDate", startDate },
        { "endDate", endDate },
        { "startTime", startTime },
        { "endTime", endTime }
    };

    var jobCount = monitor.JobCountByStateName(countParameters);
    var pager = new Pager(from, perPage, jobCount)
    {
        JobsFilterText = filterString,
        JobsFilterMethodText = filterMethodString,
        JobsFilterStartDate = startDate,
        JobsFilterEndDate = endDate,
        JobsFilterStartTime = startTime,
        JobsFilterEndTime = endTime
    };

    var processingJobs = monitor.ProcessingJobs(pager);
    var servers = monitor.Servers();


            
            #line default
            #line hidden
WriteLiteral("\r\n<div class=\"row\">\r\n    <div class=\"col-md-3\">\r\n        ");


            
            #line 53 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
   Write(Html.JobsSidebar());

            
            #line default
            #line hidden
WriteLiteral("\r\n    </div>\r\n    <div class=\"col-md-9\">\r\n        <h1 class=\"page-header\">");


            
            #line 56 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                           Write(Strings.ProcessingJobsPage_Title);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");


            
            #line 58 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
         if (pager.TotalPageCount == 0)
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"alert alert-info\">\r\n                ");


            
            #line 61 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
           Write(Strings.ProcessingJobsPage_NoJobs);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 63 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
        }
        else
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"js-jobs-list\">\r\n                <div class=\"btn-toolbar b" +
"tn-toolbar-top\">\r\n                    <button class=\"js-jobs-list-command btn bt" +
"n-sm btn-primary\"\r\n                            data-url=\"");


            
            #line 69 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                 Write(Url.To("/jobs/processing/requeue"));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            data-loading-text=\"");


            
            #line 70 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                          Write(Strings.Common_Enqueueing);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            disabled=\"disabled\">\r\n                        <spa" +
"n class=\"glyphicon glyphicon-repeat\"></span>\r\n                        ");


            
            #line 73 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                   Write(Strings.Common_RequeueJobs);

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </button>\r\n                    <button class=\"js-jobs-list-" +
"command btn btn-sm btn-default\"\r\n                            data-url=\"");


            
            #line 76 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                 Write(Url.To("/jobs/processing/delete"));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            data-loading-text=\"");


            
            #line 77 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                          Write(Strings.Common_Deleting);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            data-confirm=\"");


            
            #line 78 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                     Write(Strings.Common_DeleteConfirm);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                            disabled=\"disabled\">\r\n                        <spa" +
"n class=\"glyphicon glyphicon-remove\"></span>\r\n                        ");


            
            #line 81 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                   Write(Strings.Common_DeleteSelected);

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </button>\r\n");


            
            #line 83 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                     if (EnableSearch == true)
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <button data-toggle=\"collapse\" data-target=\"#advanced-sea" +
"rch-bar\" class=\"btn btn-sm btn-success\">Advanced Search</button>\r\n");


            
            #line 86 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                </div>\r\n");


            
            #line 88 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                 if (EnableSearch == true)
                {

            
            #line default
            #line hidden
WriteLiteral(@"                    <div id=""advanced-search-bar"" class=""collapse well"">
                        <h4 class=""advanced-search-header"">
                            Advanced Search
                        </h4>
                        <div class=""row"">
                            <div class=""col-md-12"">
                                <div class=""form-group"">
                                    <input type=""text"" value="""" id=""filterValueString"" class=""form-control"" placeholder=""Search..."" />
                                </div>
                                <div class=""form-group"">

");


            
            #line 101 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                      
                                        var currentDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                                    

            
            #line default
            #line hidden
WriteLiteral(@"
                                    <input id=""filterOnDateTime"" name=""filterOnDateTime"" type=""checkbox"" class=""js-jobs-filterOnDateTime-checked"" />
                                    <label for=""filterOnDateTime"">Filter on date time</label>
                                    <div id=""datetime-filters"" class=""row"">
                                        <div class=""col-xs-6"">
                                            <input type=""text"" value=""");


            
            #line 109 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                                 Write(currentDateTime);

            
            #line default
            #line hidden
WriteLiteral("\" class=\"datetimeselector-start form-control\" id=\"startDateTime\" />\r\n            " +
"                            </div>\r\n                                        <div" +
" class=\"col-xs-6\">\r\n                                            <input type=\"tex" +
"t\" value=\"");


            
            #line 112 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                                 Write(currentDateTime);

            
            #line default
            #line hidden
WriteLiteral(@""" class=""datetimeselector-end form-control"" id=""endDateTime"" />
                                        </div>
                                    </div>
                                </div>
                                <div class=""form-group"">
                                    <input id=""filterOnMethodName"" name=""filterOnMethodName"" type=""checkbox"" class=""js-jobs-filterOnMethodName-checked"" />
                                    <label for=""filterOnMethodName"">Filter on method name</label>
                                    <input type=""text"" value="""" id=""filterMethodString"" class=""form-control"" placeholder=""Method name..."" />
                                </div>
                            </div>
                        </div>
                        <div class=""row"">
                            <div class=""col-md-12"">
                                <button class=""js-jobs-filter-command btn btn-sm btn-success"" data-url=""");


            
            #line 125 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                                                                   Write(Url.To("/jobs/processing"));

            
            #line default
            #line hidden
WriteLiteral(@""">
                                    <span class=""glyphicon glyphicon-repeat""></span>
                                    Filter jobs
                                </button>
                            </div>
                        </div>
                    </div>
");


            
            #line 132 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("\r\n                ");


            
            #line 134 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
           Write(Html.PerPageSelector(pager));

            
            #line default
            #line hidden
WriteLiteral(@"
                <div class=""table-responsive"">
                    <table class=""table"">
                        <thead>
                            <tr>
                                <th class=""min-width"">
                                    <input type=""checkbox"" class=""js-jobs-list-select-all"" />
                                </th>
                                <th class=""min-width"">");


            
            #line 142 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                 Write(Strings.Common_Id);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"min-width\">");


            
            #line 143 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                 Write(Strings.Common_Server);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th>");


            
            #line 144 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                               Write(Strings.Common_Job);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"align-right\">");


            
            #line 145 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                   Write(Strings.ProcessingJobsPage_Table_Started);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                            </tr>\r\n                        </thead>\r\n     " +
"                   <tbody>\r\n");


            
            #line 149 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                             foreach (var job in processingJobs)
                            {

            
            #line default
            #line hidden
WriteLiteral("                                <tr class=\"js-jobs-list-row ");


            
            #line 151 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                        Write(!job.Value.InProcessingState ? " obsolete-data" : null);

            
            #line default
            #line hidden
WriteLiteral(" ");


            
            #line 151 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                                                                                  Write(job.Value.InProcessingState ? "hover" : null);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                    <td>\r\n");


            
            #line 153 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                         if (job.Value.InProcessingState)
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <input type=\"checkbox\" class=\"js-jobs" +
"-list-checkbox\" name=\"jobs[]\" value=\"");


            
            #line 155 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                                                                                 Write(job.Key);

            
            #line default
            #line hidden
WriteLiteral("\" />\r\n");


            
            #line 156 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                        }

            
            #line default
            #line hidden
WriteLiteral("                                    </td>\r\n                                    <t" +
"d class=\"min-width\">\r\n                                        ");


            
            #line 159 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                   Write(Html.JobIdLink(job.Key));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 160 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                         if (!job.Value.InProcessingState)
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <span title=\"");


            
            #line 162 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                    Write(Strings.Common_JobStateChanged_Text);

            
            #line default
            #line hidden
WriteLiteral("\" class=\"glyphicon glyphicon-question-sign\"></span>\r\n");


            
            #line 163 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                        }

            
            #line default
            #line hidden
WriteLiteral("                                    </td>\r\n                                    <t" +
"d class=\"min-width\">\r\n                                        ");


            
            #line 166 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                   Write(Html.ServerId(job.Value.ServerId));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                    </td>\r\n                                    " +
"<td class=\"word-break\">\r\n");


            
            #line 169 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                         if (servers.All(x => x.Name != job.Value.ServerId || x.Heartbeat < DateTime.UtcNow.AddMinutes(-1)))
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <span title=\"");


            
            #line 171 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                    Write(Strings.ProcessingJobsPage_Aborted);

            
            #line default
            #line hidden
WriteLiteral("\" class=\"glyphicon glyphicon-warning-sign\" style=\"font-size: 10px;\"></span>\r\n");


            
            #line 172 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                        }

            
            #line default
            #line hidden
WriteLiteral("\r\n                                        ");


            
            #line 174 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                   Write(Html.JobNameLink(job.Key, job.Value.Job));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                    </td>\r\n                                    " +
"<td class=\"align-right\">\r\n");


            
            #line 177 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                         if (job.Value.StartedAt.HasValue)
                                        {
                                            if (RelativeTime == true)
                                            {
                                                
            
            #line default
            #line hidden
            
            #line 181 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                           Write(Html.RelativeTime(job.Value.StartedAt.Value));

            
            #line default
            #line hidden
            
            #line 181 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                                                             ;
                                            }
                                            else
                                            {
                                                
            
            #line default
            #line hidden
            
            #line 185 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                           Write(Html.Raw(job.Value.StartedAt.Value.ToString("dd/MM/yyyy HH:mm")));

            
            #line default
            #line hidden
            
            #line 185 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                                                                                                                 ;
                                            }
                                        }

            
            #line default
            #line hidden
WriteLiteral("                                    </td>\r\n                                </tr>\r" +
"\n");


            
            #line 190 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
                            }

            
            #line default
            #line hidden
WriteLiteral("                        </tbody>\r\n                    </table>\r\n                <" +
"/div>\r\n\r\n                ");


            
            #line 195 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
           Write(Html.Paginator(pager));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 197 "..\..\Dashboard\Pages\ProcessingJobsPage.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("    </div>\r\n</div>");


        }
    }
}
#pragma warning restore 1591
