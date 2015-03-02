﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Hangfire.Dashboard.Pages
{
    
    #line 2 "..\..\Dashboard\Pages\ServersPage.cshtml"
    using System;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Dashboard\Pages\ServersPage.cshtml"
    using System.Collections.Generic;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Dashboard\Pages\ServersPage.cshtml"
    using System.Linq;
    
    #line default
    #line hidden
    using System.Text;
    
    #line 5 "..\..\Dashboard\Pages\ServersPage.cshtml"
    using Hangfire.Common;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Dashboard\Pages\ServersPage.cshtml"
    using Hangfire.Dashboard;
    
    #line default
    #line hidden
    
    #line 7 "..\..\Dashboard\Pages\ServersPage.cshtml"
    using Hangfire.Dashboard.Pages;
    
    #line default
    #line hidden
    
    #line 8 "..\..\Dashboard\Pages\ServersPage.cshtml"
    using Hangfire.Storage.Monitoring;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    internal partial class ServersPage : RazorPage
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");










            
            #line 10 "..\..\Dashboard\Pages\ServersPage.cshtml"
  
    Layout = new LayoutPage { Title = "Servers" };
    
    var monitor = Storage.GetMonitoringApi();
    IList<ServerDto> servers = monitor.Servers();    


            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 17 "..\..\Dashboard\Pages\ServersPage.cshtml"
 if (servers.Count == 0)
{

            
            #line default
            #line hidden
WriteLiteral("    <div class=\"alert alert-warning\">\r\n        There are no active servers. Backg" +
"round tasks will not be processed.\r\n    </div>\r\n");


            
            #line 22 "..\..\Dashboard\Pages\ServersPage.cshtml"
}
else
{

            
            #line default
            #line hidden
WriteLiteral(@"    <table class=""table"">
        <thead>
            <tr>
                <th>Name</th>
                <th>Workers</th>
                <th>Queues</th>
                <th>Started</th>
                <th>Heartbeat</th>
            </tr>
        </thead>
        <tbody>
");


            
            #line 36 "..\..\Dashboard\Pages\ServersPage.cshtml"
             foreach (var server in servers)
            {

            
            #line default
            #line hidden
WriteLiteral("                <tr>\r\n                    <td>");


            
            #line 39 "..\..\Dashboard\Pages\ServersPage.cshtml"
                   Write(server.Name.ToUpperInvariant());

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                    <td>");


            
            #line 40 "..\..\Dashboard\Pages\ServersPage.cshtml"
                   Write(server.WorkersCount);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                    <td>");


            
            #line 41 "..\..\Dashboard\Pages\ServersPage.cshtml"
                   Write(HtmlHelper.Raw(String.Join(" ", server.Queues.Select(HtmlHelper.QueueLabel))));

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                    <td data-moment=\"");


            
            #line 42 "..\..\Dashboard\Pages\ServersPage.cshtml"
                                Write(JobHelper.ToTimestamp(server.StartedAt));

            
            #line default
            #line hidden
WriteLiteral("\">");


            
            #line 42 "..\..\Dashboard\Pages\ServersPage.cshtml"
                                                                          Write(server.StartedAt);

            
            #line default
            #line hidden
WriteLiteral("</td>\r\n                    <td>\r\n");


            
            #line 44 "..\..\Dashboard\Pages\ServersPage.cshtml"
                         if (server.Heartbeat.HasValue)
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <span data-moment=\"");


            
            #line 46 "..\..\Dashboard\Pages\ServersPage.cshtml"
                                          Write(JobHelper.ToTimestamp(server.Heartbeat.Value));

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                ");


            
            #line 47 "..\..\Dashboard\Pages\ServersPage.cshtml"
                           Write(server.Heartbeat);

            
            #line default
            #line hidden
WriteLiteral("\r\n                            </span>\r\n");


            
            #line 49 "..\..\Dashboard\Pages\ServersPage.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </td>\r\n                </tr>\r\n");


            
            #line 52 "..\..\Dashboard\Pages\ServersPage.cshtml"
            }

            
            #line default
            #line hidden
WriteLiteral("        </tbody>\r\n    </table>\r\n");


            
            #line 55 "..\..\Dashboard\Pages\ServersPage.cshtml"
}
            
            #line default
            #line hidden

        }
    }
}
#pragma warning restore 1591
