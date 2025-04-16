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
    var dto = job != null ? new JobDetailsRendererDto(this, JobId, job) : null;
    
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


            
            #line 37 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
   Write(Html.JobsSidebar());

            
            #line default
            #line hidden
WriteLiteral("\r\n    </div>\r\n    <div class=\"col-md-9\">\r\n        <h1 class=\"page-header\">");


            
            #line 40 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                           Write(title);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");


            
            #line 42 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
         if (job == null)
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"alert alert-warning\">\r\n                ");


            
            #line 45 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
           Write(String.Format(Strings.JobDetailsPage_JobExpired, JobId));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 47 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
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


            
            #line 57 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                   Write(Html.Raw(String.Format(Strings.JobDetailsPage_JobAbortedNotActive_Warning_Html, currentState.Data["ServerId"], Url.To("/servers"))));

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </div>\r\n");


            
            #line 59 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                }
                else if (server.Heartbeat.HasValue && server.Heartbeat < (StorageUtcNow ?? ApplicationUtcNow).Add(DashboardOptions.ServerPossiblyAbortedThreshold.Negate()))
                {

            
            #line default
            #line hidden
WriteLiteral("                    <div class=\"alert alert-warning\">\r\n                        ");


            
            #line 63 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                   Write(Html.Raw(String.Format(Strings.JobDetailsPage_JobAbortedWithHeartbeat_Warning_Html, server.Name)));

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </div>\r\n");


            
            #line 65 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                }
            }

            if (job.ExpireAt.HasValue)
            {

            
            #line default
            #line hidden
WriteLiteral("                <div class=\"alert alert-info\">\r\n                    ");


            
            #line 71 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
               Write(Html.Raw(String.Format(Strings.JobDetailsPage_JobFinished_Warning_Html, JobHelper.ToTimestamp(job.ExpireAt.Value), job.ExpireAt)));

            
            #line default
            #line hidden
WriteLiteral("\r\n                </div>\r\n");


            
            #line 73 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
            }

            if (job.Job != null)
            {

            
            #line default
            #line hidden
WriteLiteral("                <div class=\"job-snippet\">\r\n                    <div class=\"job-sn" +
"ippet-code\">\r\n                        <pre><code><span class=\"comment\">// ");


            
            #line 79 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                       Write(Strings.JobDetailsPage_JobId);

            
            #line default
            #line hidden
WriteLiteral(": ");


            
            #line 79 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                                                      Write(Html.JobId(JobId.ToString(), false));

            
            #line default
            #line hidden
WriteLiteral("</span>\r\n");


            
            #line 80 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
 if (job.Job.Queue != null)
{

            
            #line default
            #line hidden
WriteLiteral("<span class=\"comment\">// ");


            
            #line 82 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                    Write(Strings.QueuesPage_Table_Queue);

            
            #line default
            #line hidden
WriteLiteral(": ");


            
            #line 82 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                     Write(job.Job.Queue);

            
            #line default
            #line hidden
WriteLiteral("</span>\r\n");


            
            #line 83 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
}

            
            #line default
            #line hidden

            
            #line 84 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
Write(JobMethodCallRenderer.Render(job.Job));

            
            #line default
            #line hidden
WriteLiteral("\r\n</code></pre>\r\n                    </div>\r\n                </div>\r\n");


            
            #line 88 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
            }
            else
            {
                var dbgParameters = job.Properties.Where(x => x.Key.StartsWith("DBG_")).ToArray();


            
            #line default
            #line hidden
WriteLiteral("                <div class=\"alert alert-warning\">\r\n                    <h4 id=\"jo" +
"b-details-missing-method\">");


            
            #line 94 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                   Write(Strings.Common_CannotFindTargetMethod);

            
            #line default
            #line hidden
WriteLiteral("</h4>\r\n                    <table class=\"table table-condensed job-snippet-proper" +
"ties margin-bottom-0\" aria-describedby=\"job-details-missing-method\">\r\n          " +
"              <tr>\r\n                            <td>");


            
            #line 97 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                           Write(Strings.JobDetailsPage_JobId);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                            <td><pre><code>");


            
            #line 98 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                      Write(Html.JobId(JobId, false));

            
            #line default
            #line hidden
WriteLiteral("</code></pre></td>\r\n                        </tr>\r\n");


            
            #line 100 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                         foreach (var parameter in dbgParameters)
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <tr>\r\n                                <td class=\"widt" +
"h-15\">");


            
            #line 103 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                Write(parameter.Key.Substring(4));

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                                <td><pre><code>");


            
            #line 104 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                          Write(parameter.Value);

            
            #line default
            #line hidden
WriteLiteral("</code></pre></td>\r\n                            </tr>\r\n");


            
            #line 106 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </table>\r\n                </div>\r\n");


            
            #line 109 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
            }

            var displayParameters = job.Properties.Where(x => !x.Key.StartsWith("DBG_") && !x.Key.StartsWith("SRC_") && x.Key != "Continuations").ToArray();

            if (displayParameters.Length > 0)
            {

            
            #line default
            #line hidden
WriteLiteral("                <h3 id=\"job-details-parameters\">");


            
            #line 115 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                           Write(Strings.JobDetailsPage_Parameters);

            
            #line default
            #line hidden
WriteLiteral("</h3>\r\n");



WriteLiteral("                <table class=\"table table-condensed job-snippet-properties\" aria-" +
"describedby=\"job-details-parameters\">\r\n");


            
            #line 117 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                     foreach (var parameter in displayParameters)
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <tr>\r\n                            <td class=\"width-20 wor" +
"d-break\">");


            
            #line 120 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                       Write(parameter.Key);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                            <td><pre><code>");


            
            #line 121 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                      Write(parameter.Value);

            
            #line default
            #line hidden
WriteLiteral("</code></pre></td>\r\n                        </tr>\r\n");


            
            #line 123 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                </table>\r\n");


            
            #line 125 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
            }

            if (job.Properties.TryGetValue("Continuations", out var serializedContinuations))
            {
                var continuations = ContinuationsSupportAttribute.DeserializeContinuations(serializedContinuations);

                if (continuations.Count > 0)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <h3 id=\"job-details-continuations\">");


            
            #line 133 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                  Write(Strings.Common_Continuations);

            
            #line default
            #line hidden
WriteLiteral("</h3>\r\n");



WriteLiteral(@"                    <div class=""table-responsive"">
                        <table class=""table"" aria-describedby=""job-details-continuations"">
                            <thead>
                            <tr>
                                <th class=""min-width"">");


            
            #line 138 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                 Write(Strings.Common_Id);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"min-width\">");


            
            #line 139 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                 Write(Strings.Common_Condition);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"min-width\">");


            
            #line 140 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                 Write(Strings.Common_State);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th>");


            
            #line 141 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                               Write(Strings.Common_Job);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"align-right\">");


            
            #line 142 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                   Write(Strings.Common_Created);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                            </tr>\r\n                            </thead>\r\n " +
"                           <tbody>\r\n");


            
            #line 146 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                             foreach (var continuation in continuations)
                            {
                                JobData jobData;

                                using (var connection = Storage.GetReadOnlyConnection())
                                {
                                    jobData = connection.GetJobData(continuation.JobId);
                                }


            
            #line default
            #line hidden
WriteLiteral("                                <tr>\r\n");


            
            #line 156 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                     if (jobData == null)
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <td colspan=\"5\"><em>");


            
            #line 158 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                       Write(String.Format(Strings.JobDetailsPage_JobExpired, continuation.JobId));

            
            #line default
            #line hidden
WriteLiteral("</em></td>\r\n");


            
            #line 159 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                    }
                                    else
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <td class=\"min-width\">");


            
            #line 162 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                         Write(Html.JobIdLink(continuation.JobId));

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n");



WriteLiteral("                                        <td class=\"min-width\"><code>");


            
            #line 163 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                               Write(continuation.Options.ToString("G"));

            
            #line default
            #line hidden
WriteLiteral("</code></td>\r\n");



WriteLiteral("                                        <td class=\"min-width\">");


            
            #line 164 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                         Write(Html.StateLabel(jobData.State));

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n");



WriteLiteral("                                        <td class=\"word-break\">");


            
            #line 165 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                          Write(Html.JobNameLink(continuation.JobId, jobData.Job));

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n");



WriteLiteral("                                        <td class=\"align-right\">");


            
            #line 166 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                           Write(Html.RelativeTime(jobData.CreatedAt));

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n");


            
            #line 167 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                </tr>\r\n");


            
            #line 169 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                            }

            
            #line default
            #line hidden
WriteLiteral("                            </tbody>\r\n                        </table>\r\n         " +
"           </div>\r\n");


            
            #line 173 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                }
            }

            if (dto != null)
            {
                foreach (var renderer in JobDetailsRenderer.GetRenderers())
                {
                    try
                    {
                        
            
            #line default
            #line hidden
            
            #line 182 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                   Write(renderer.Item2(dto));

            
            #line default
            #line hidden
            
            #line 182 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                            
                    }
                    catch (Exception ex)
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <h4 class=\"exception-type\">");


            
            #line 186 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                              Write(ex.GetType().Name);

            
            #line default
            #line hidden
WriteLiteral("</h4>\r\n");



WriteLiteral("                        <p class=\"text-muted\">");


            
            #line 187 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                         Write(ex.Message);

            
            #line default
            #line hidden
WriteLiteral("</p>\r\n");



WriteLiteral("                        <pre class=\"stack-trace\">");


            
            #line 188 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                            Write(Html.StackTrace(ex.StackTrace));

            
            #line default
            #line hidden
WriteLiteral("</pre>\r\n");


            
            #line 189 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                    }
                }
            }

            var srcParameters = job.Properties.Where(x => x.Key.StartsWith("SRC_")).ToArray();
            if (job.Job != null && srcParameters.Length > 0)
            {

            
            #line default
            #line hidden
WriteLiteral("                <h3 id=\"job-details-source\">");


            
            #line 196 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                       Write(Strings.JobDetailsPage_Source);

            
            #line default
            #line hidden
WriteLiteral("</h3>\r\n");



WriteLiteral("                <table class=\"table table-condensed job-snippet-properties\" aria-" +
"describedby=\"job-details-source\">\r\n");


            
            #line 198 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                     foreach (var parameter in srcParameters)
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <tr>\r\n                            <td class=\"width-20 wor" +
"d-break\">");


            
            #line 201 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                       Write(parameter.Key.Substring(4));

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                            <td><pre><code>");


            
            #line 202 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                      Write(parameter.Value);

            
            #line default
            #line hidden
WriteLiteral("</code></pre></td>\r\n                        </tr>\r\n");


            
            #line 204 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                </table>\r\n");


            
            #line 206 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
            }


            
            #line default
            #line hidden
WriteLiteral("            <h3>\r\n");


            
            #line 209 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                 if (job.History.Count > 1)
                {

            
            #line default
            #line hidden
WriteLiteral("                    <span class=\"job-snippet-buttons pull-right\">\r\n");


            
            #line 212 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                         if (!IsReadOnly)
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <button class=\"btn btn-sm btn-default\"\r\n             " +
"                       data-ajax=\"");


            
            #line 215 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                          Write(Url.To("/jobs/actions/requeue/" + JobId));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                    data-loading-text=\"");


            
            #line 216 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                  Write(Strings.Common_Enqueueing);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                ");


            
            #line 217 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                           Write(Strings.JobDetailsPage_Requeue);

            
            #line default
            #line hidden
WriteLiteral("\r\n                            </button>\r\n");


            
            #line 219 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                        }

            
            #line default
            #line hidden

            
            #line 220 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                         if (!IsReadOnly)
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <button class=\"btn btn-sm btn-death\"\r\n               " +
"                     data-ajax=\"");


            
            #line 223 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                          Write(Url.To("/jobs/actions/delete/" + JobId));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                    data-loading-text=\"");


            
            #line 224 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                  Write(Strings.Common_Deleting);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                    data-confirm=\"");


            
            #line 225 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                             Write(Strings.JobDetailsPage_DeleteConfirm);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                ");


            
            #line 226 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                           Write(Strings.Common_Delete);

            
            #line default
            #line hidden
WriteLiteral("\r\n                            </button>\r\n");


            
            #line 228 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </span>\r\n");


            
            #line 230 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                }

            
            #line default
            #line hidden
WriteLiteral("\r\n                ");


            
            #line 232 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
           Write(Strings.JobDetailsPage_State);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </h3>\r\n");


            
            #line 234 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"

            var index = 0;

            foreach (var entry in job.History)
            {
                var accentColor = JobHistoryRenderer.GetForegroundStateColor(entry.StateName);
                var backgroundColor = JobHistoryRenderer.GetBackgroundStateColor(entry.StateName);
                var cardCss = index == 0 ? JobHistoryRenderer.GetStateCssSuffix(entry.StateName) : null;
                var cardStyle = index == 0 && cardCss == null ? $"border-color: {accentColor}" : null;
                var cardTitleStyle = index == 0 && cardCss == null ? $"color: {accentColor}" : null;
                var cardBackgroundStyle = index == 0 && cardCss == null ? $"background-color: {backgroundColor}" : null;


            
            #line default
            #line hidden
WriteLiteral("                <div class=\"state-card ");


            
            #line 246 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                   Write(cardCss != null ? "state-card-state-" + cardCss : null);

            
            #line default
            #line hidden
WriteLiteral("\" style=\"");


            
            #line 246 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                                                                    Write(cardStyle);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                    <h4 class=\"state-card-title\" style=\"");


            
            #line 247 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                   Write(cardTitleStyle);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                        <small class=\"pull-right text-muted\">\r\n");


            
            #line 249 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                             if (index == job.History.Count - 1)
                            {
                                
            
            #line default
            #line hidden
            
            #line 251 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                           Write(Html.RelativeTime(entry.CreatedAt));

            
            #line default
            #line hidden
            
            #line 251 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                                   
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


            
            #line 259 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                  Write(Html.RelativeTime(entry.CreatedAt));

            
            #line default
            #line hidden
WriteLiteral(" (");


            
            #line 259 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                                       Write(duration);

            
            #line default
            #line hidden
WriteLiteral(")\r\n");


            
            #line 260 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                }
                                else
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    ");

WriteLiteral(" ");


            
            #line 263 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                  Write(Html.MomentTitle(entry.CreatedAt, duration));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 264 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                }
                            }

            
            #line default
            #line hidden
WriteLiteral("                        </small>\r\n\r\n                        ");


            
            #line 268 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                   Write(entry.StateName);

            
            #line default
            #line hidden
WriteLiteral("\r\n                    </h4>\r\n\r\n");


            
            #line 271 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                     if (!String.IsNullOrWhiteSpace(entry.Reason))
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <p class=\"state-card-text text-muted\">");


            
            #line 273 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                         Write(entry.Reason);

            
            #line default
            #line hidden
WriteLiteral("</p>\r\n");


            
            #line 274 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 276 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                      
                        var rendered = Html.RenderHistory(entry.StateName, entry.Data);
                    

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 280 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                     if (rendered != null)
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <div class=\"state-card-body\" style=\"");


            
            #line 282 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                                                       Write(cardBackgroundStyle);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                            ");


            
            #line 283 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                       Write(rendered);

            
            #line default
            #line hidden
WriteLiteral("\r\n                        </div>\r\n");


            
            #line 285 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                </div>\r\n");


            
            #line 287 "..\..\Dashboard\Pages\JobDetailsPage.cshtml"

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
