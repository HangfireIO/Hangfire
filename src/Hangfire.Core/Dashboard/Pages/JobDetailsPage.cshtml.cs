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
    
    #line 2 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
    using System;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
    using System.Collections.Generic;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
    using System.Linq;
    
    #line default
    #line hidden
    using System.Text;
    
    #line 5 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
    using Hangfire;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
    using Hangfire.Common;
    
    #line default
    #line hidden
    
    #line 7 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
    using Hangfire.Dashboard;
    
    #line default
    #line hidden
    
    #line 8 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
    using Hangfire.Dashboard.Pages;
    
    #line default
    #line hidden
    
    #line 9 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
    using Hangfire.Dashboard.Resources;
    
    #line default
    #line hidden
    
    #line 10 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
    using Hangfire.States;
    
    #line default
    #line hidden
    
    #line 11 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
    using Hangfire.Storage;
    
    #line default
    #line hidden
    
    #line 12 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
    using Hangfire.Storage.Monitoring;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    internal partial class JobDetailsPage : RazorPage
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");














            
            #line 14 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
  
    var monitor = Storage.GetMonitoringApi();
    var job = monitor.JobDetails(JobId);
    
    string title = null;

    if (job != null)
    {
        title = job.Job != null ? Html.JobName(job.Job) : null;

        var historyList = new List<StateHistoryDto>(job.History);
        historyList.Add(new StateHistoryDto { StateName = "Created", CreatedAt = job.CreatedAt ?? default(DateTime) });

        job.History = historyList;
    }

    title = title ?? Strings.Common_Job;
    Layout = new LayoutPage(title);


            
            #line default
            #line hidden
WriteLiteral("\r\n<div class=\"row\">\r\n    <div class=\"col-md-3\">\r\n        ");


            
            #line 36 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
   Write(Html.JobsSidebar());

            
            #line default
            #line hidden
WriteLiteral("\r\n    </div>\r\n    <div class=\"col-md-9\">\r\n        <h1 class=\"page-header\">");


            
            #line 39 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                           Write(title);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");


            
            #line 41 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
         if (job == null)
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"alert alert-warning\">\r\n                ");


            
            #line 44 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
           Write(String.Format(Strings.JobDetailsPage_JobExpired, JobId));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 46 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
        }
        else
        {
            var currentState = job.History[0];
            if (currentState.StateName == ProcessingState.StateName)
            {
                var server = monitor.Servers().FirstOrDefault(x => x.Name == currentState.Data["ServerId"]);
                if (server == null)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <div class=\"alert alert-danger\">\r\n                        ");


            
            #line 56 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                   Write(Html.Raw(String.Format(Strings.JobDetailsPage_JobAbortedNotActive_Warning_Html, currentState.Data["ServerId"], Url.To("/servers"))));

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </div>\r\n");


            
            #line 58 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                }
                else if (server.Heartbeat.HasValue && server.Heartbeat < DateTime.UtcNow.AddMinutes(-1))
                {

            
            #line default
            #line hidden
WriteLiteral("                    <div class=\"alert alert-warning\">\r\n                        ");


            
            #line 62 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                   Write(Html.Raw(String.Format(Strings.JobDetailsPage_JobAbortedWithHeartbeat_Warning_Html, server.Name)));

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </div>\r\n");


            
            #line 64 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                }
            }

            if (job.ExpireAt.HasValue)
            {

            
            #line default
            #line hidden
WriteLiteral("                <div class=\"alert alert-info\">\r\n                    ");


            
            #line 70 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
               Write(Html.Raw(String.Format(Strings.JobDetailsPage_JobFinished_Warning_Html, JobHelper.ToTimestamp(job.ExpireAt.Value), job.ExpireAt)));

            
            #line default
            #line hidden
WriteLiteral("\r\n                </div>\r\n");


            
            #line 72 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
            }

            if (job.Job != null)
            {

            
            #line default
            #line hidden
WriteLiteral("                <div class=\"job-snippet\">\r\n                    <div class=\"job-sn" +
"ippet-code\">\r\n                        <pre><code><span class=\"comment\">// ");


            
            #line 78 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                       Write(Strings.JobDetailsPage_JobId);

            
            #line default
            #line hidden
WriteLiteral(": ");


            
            #line 78 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                                                      Write(Html.JobId(JobId.ToString(), false));

            
            #line default
            #line hidden
WriteLiteral("</span>\r\n");


            
            #line 79 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
Write(JobMethodCallRenderer.Render(job.Job));

            
            #line default
            #line hidden
WriteLiteral("\r\n</code></pre>\r\n                    </div>\r\n                </div>\r\n");


            
            #line 83 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
            }
            else
            {
                var dbgParameters = job.Properties.Where(x => x.Key.StartsWith("DBG_")).ToArray();


            
            #line default
            #line hidden
WriteLiteral("                <div class=\"alert alert-warning\">\r\n                    <h4>");


            
            #line 89 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                   Write(Strings.Common_CannotFindTargetMethod);

            
            #line default
            #line hidden
WriteLiteral("</h4>\r\n                    <table class=\"table table-condensed job-snippet-proper" +
"ties\" style=\"margin-bottom: 0;\">\r\n                        <tr>\r\n                " +
"            <td>");


            
            #line 92 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                           Write(Strings.JobDetailsPage_JobId);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                            <td><pre><code>");


            
            #line 93 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                      Write(Html.JobId(JobId, false));

            
            #line default
            #line hidden
WriteLiteral("</code></pre></td>\r\n                        </tr>\r\n");


            
            #line 95 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                         foreach (var parameter in dbgParameters)
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <tr>\r\n                                <td style=\"widt" +
"h: 15%;\">");


            
            #line 98 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                   Write(parameter.Key.Substring(4));

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                                <td><pre><code>");


            
            #line 99 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                          Write(parameter.Value);

            
            #line default
            #line hidden
WriteLiteral("</code></pre></td>\r\n                            </tr>\r\n");


            
            #line 101 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </table>\r\n                </div>\r\n");


            
            #line 104 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
            }

            var displayParameters = job.Properties.Where(x => !x.Key.StartsWith("DBG_") && x.Key != "Continuations").ToArray();

            if (displayParameters.Length > 0)
            {

            
            #line default
            #line hidden
WriteLiteral("                <h3>");


            
            #line 110 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
               Write(Strings.JobDetailsPage_Parameters);

            
            #line default
            #line hidden
WriteLiteral("</h3>\r\n");



WriteLiteral("                <table class=\"table table-condensed job-snippet-properties\">\r\n");


            
            #line 112 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                     foreach (var parameter in displayParameters)
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <tr>\r\n                            <td style=\"width: 20%; " +
"word-break: break-all;\">");


            
            #line 115 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                                      Write(parameter.Key);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                            <td><pre><code>");


            
            #line 116 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                      Write(parameter.Value);

            
            #line default
            #line hidden
WriteLiteral("</code></pre></td>\r\n                        </tr>\r\n");


            
            #line 118 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                </table>\r\n");


            
            #line 120 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
            }

            if (job.Properties.TryGetValue("Continuations", out var serializedContinuations))
            {
                var continuations = ContinuationsSupportAttribute.DeserializeContinuations(serializedContinuations);

                if (continuations.Count > 0)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <h3>");


            
            #line 128 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                   Write(Strings.Common_Continuations);

            
            #line default
            #line hidden
WriteLiteral("</h3>\r\n");



WriteLiteral("                    <div class=\"table-responsive\">\r\n                        <tabl" +
"e class=\"table\">\r\n                            <thead>\r\n                         " +
"   <tr>\r\n                                <th class=\"min-width\">");


            
            #line 133 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                 Write(Strings.Common_Id);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"min-width\">");


            
            #line 134 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                 Write(Strings.Common_Condition);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"min-width\">");


            
            #line 135 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                 Write(Strings.Common_State);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th>");


            
            #line 136 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                               Write(Strings.Common_Job);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"align-right\">");


            
            #line 137 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                   Write(Strings.Common_Created);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                            </tr>\r\n                            </thead>\r\n " +
"                           <tbody>\r\n");


            
            #line 141 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                             foreach (var continuation in continuations)
                            {
                                JobData jobData;

                                using (var connection = Storage.GetConnection())
                                {
                                    jobData = connection.GetJobData(continuation.JobId);
                                }


            
            #line default
            #line hidden
WriteLiteral("                                <tr>\r\n");


            
            #line 151 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                     if (jobData == null)
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <td colspan=\"5\"><em>");


            
            #line 153 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                       Write(String.Format(Strings.JobDetailsPage_JobExpired, continuation.JobId));

            
            #line default
            #line hidden
WriteLiteral("</em></td>\r\n");


            
            #line 154 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                    }
                                    else
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <td class=\"min-width\">");


            
            #line 157 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                         Write(Html.JobIdLink(continuation.JobId));

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n");



WriteLiteral("                                        <td class=\"min-width\"><code>");


            
            #line 158 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                               Write(continuation.Options.ToString("G"));

            
            #line default
            #line hidden
WriteLiteral("</code></td>\r\n");



WriteLiteral("                                        <td class=\"min-width\">");


            
            #line 159 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                         Write(Html.StateLabel(jobData.State));

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n");



WriteLiteral("                                        <td class=\"word-break\">");


            
            #line 160 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                          Write(Html.JobNameLink(continuation.JobId, jobData.Job));

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n");



WriteLiteral("                                        <td class=\"align-right\">");


            
            #line 161 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                           Write(Html.RelativeTime(jobData.CreatedAt));

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n");


            
            #line 162 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                </tr>\r\n");


            
            #line 164 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                            }

            
            #line default
            #line hidden
WriteLiteral("                            </tbody>\r\n                        </table>\r\n         " +
"           </div>\r\n");


            
            #line 168 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                }
            }


            
            #line default
            #line hidden
WriteLiteral("            <h3>\r\n");


            
            #line 172 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                 if (job.History.Count > 1)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <span class=\"job-snippet-buttons pull-right\">\r\n");


            
            #line 175 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                         if (!IsReadOnly)
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <button class=\"btn btn-sm btn-default\"\r\n             " +
"                       data-ajax=\"");


            
            #line 178 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                          Write(Url.To("/jobs/actions/requeue/" + JobId));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                    data-loading-text=\"");


            
            #line 179 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                  Write(Strings.Common_Enqueueing);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                ");


            
            #line 180 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                           Write(Strings.JobDetailsPage_Requeue);

            
            #line default
            #line hidden
WriteLiteral("\r\n                            </button>\r\n");


            
            #line 182 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                        }

            
            #line default
            #line hidden

            
            #line 183 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                         if (!IsReadOnly)
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <button class=\"btn btn-sm btn-death\"\r\n               " +
"                     data-ajax=\"");


            
            #line 186 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                          Write(Url.To("/jobs/actions/delete/" + JobId));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                    data-loading-text=\"");


            
            #line 187 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                  Write(Strings.Common_Deleting);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                    data-confirm=\"");


            
            #line 188 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                             Write(Strings.JobDetailsPage_DeleteConfirm);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                ");


            
            #line 189 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                           Write(Strings.Common_Delete);

            
            #line default
            #line hidden
WriteLiteral("\r\n                            </button>\r\n");


            
            #line 191 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </span>\r\n");


            
            #line 193 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("\r\n                ");


            
            #line 195 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
           Write(Strings.JobDetailsPage_State);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </h3>\r\n");


            
            #line 197 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"

            var index = 0;

            foreach (var entry in job.History)
            {

            
            #line default
            #line hidden
WriteLiteral("                <div class=\"state-card ");


            
            #line 202 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                   Write(index == 0 ? $"state-card_state_{entry.StateName.ToLower()}" : null);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                    <h4 class=\"state-card-title\">\r\n                        <s" +
"mall class=\"pull-right text-muted\">\r\n");


            
            #line 205 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                             if (index == job.History.Count - 1)
                            {
                                
            
            #line default
            #line hidden
            
            #line 207 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                           Write(Html.RelativeTime(entry.CreatedAt));

            
            #line default
            #line hidden
            
            #line 207 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                                   
                            }
                            else
                            {
                                var duration = Html.ToHumanDuration(entry.CreatedAt - job.History[index + 1].CreatedAt);

                                if (index == 0)
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    ");

WriteLiteral(" ");


            
            #line 215 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                  Write(Html.RelativeTime(entry.CreatedAt));

            
            #line default
            #line hidden
WriteLiteral(" (");


            
            #line 215 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                                       Write(duration);

            
            #line default
            #line hidden
WriteLiteral(")\r\n");


            
            #line 216 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                }
                                else
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    ");

WriteLiteral(" ");


            
            #line 219 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                  Write(Html.MomentTitle(entry.CreatedAt, duration));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 220 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                }
                            }

            
            #line default
            #line hidden
WriteLiteral("                        </small>\r\n\r\n                        ");


            
            #line 224 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                   Write(entry.StateName);

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </h4>\r\n\r\n");


            
            #line 227 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                     if (!String.IsNullOrWhiteSpace(entry.Reason))
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <p class=\"state-card-text text-muted\">");


            
            #line 229 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                         Write(entry.Reason);

            
            #line default
            #line hidden
WriteLiteral("</p>\r\n");


            
            #line 230 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 232 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                      
                        var rendered = Html.RenderHistory(entry.StateName, entry.Data);
                    

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 236 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                     if (rendered != null)
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <div class=\"state-card-body\">\r\n                          " +
"  ");


            
            #line 239 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                       Write(rendered);

            
            #line default
            #line hidden
WriteLiteral("\r\n                        </div>\r\n");


            
            #line 241 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                </div>\r\n");


            
            #line 243 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"

                index++;
            }
        }

            
            #line default
            #line hidden
WriteLiteral("    </div>\r\n</div>");


        }
    }
}
#pragma warning restore 1591
