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
    
    #line 2 "..\..\Dashboard\Pages\LayoutPage.cshtml"
    using System;
    
    #line default
    #line hidden
    using System.Collections.Generic;
    
    #line 3 "..\..\Dashboard\Pages\LayoutPage.cshtml"
    using System.Globalization;
    
    #line default
    #line hidden
    using System.Linq;
    
    #line 4 "..\..\Dashboard\Pages\LayoutPage.cshtml"
    using System.Reflection;
    
    #line default
    #line hidden
    using System.Text;
    
    #line 5 "..\..\Dashboard\Pages\LayoutPage.cshtml"
    using Hangfire.Dashboard;
    
    #line default
    #line hidden
    
    #line 6 "..\..\Dashboard\Pages\LayoutPage.cshtml"
    using Hangfire.Dashboard.Pages;
    
    #line default
    #line hidden
    
    #line 7 "..\..\Dashboard\Pages\LayoutPage.cshtml"
    using Hangfire.Dashboard.Resources;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    public partial class LayoutPage : RazorPage
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");








WriteLiteral("<!DOCTYPE html>\r\n<html lang=\"");


            
            #line 10 "..\..\Dashboard\Pages\LayoutPage.cshtml"
       Write(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n<head>\r\n    <title>");


            
            #line 12 "..\..\Dashboard\Pages\LayoutPage.cshtml"
      Write(Title);

            
            #line default
            #line hidden
WriteLiteral(" – ");


            
            #line 12 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                Write(DashboardOptions.DashboardTitle.Contains("<") ? "Hangfire Dashboard" : DashboardOptions.DashboardTitle);

            
            #line default
            #line hidden
WriteLiteral("</title>\r\n    <meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">\r\n    <meta ch" +
"arset=\"utf-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-s" +
"cale=1.0\">\r\n    <meta name=\"robots\" content=\"none\">\r\n");


            
            #line 17 "..\..\Dashboard\Pages\LayoutPage.cshtml"
     if (!DashboardOptions.IgnoreAntiforgeryToken)
    {
        if (!String.IsNullOrWhiteSpace(Context.AntiforgeryHeader))
        {

            
            #line default
            #line hidden
WriteLiteral("            ");

WriteLiteral(" <meta name=\"csrf-header\" content=\"");


            
            #line 21 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                            Write(Context.AntiforgeryHeader);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n");


            
            #line 22 "..\..\Dashboard\Pages\LayoutPage.cshtml"
        }
        if (!String.IsNullOrWhiteSpace(Context.AntiforgeryToken))
        {

            
            #line default
            #line hidden
WriteLiteral("            ");

WriteLiteral(" <meta name=\"csrf-token\" content=\"");


            
            #line 25 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                           Write(Context.AntiforgeryToken);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n");


            
            #line 26 "..\..\Dashboard\Pages\LayoutPage.cshtml"
        }
    }

            
            #line default
            #line hidden

            
            #line 28 "..\..\Dashboard\Pages\LayoutPage.cshtml"
       var version = GetType().GetTypeInfo().Assembly.GetName().Version; 

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 30 "..\..\Dashboard\Pages\LayoutPage.cshtml"
     if(!string.IsNullOrWhiteSpace(DashboardOptions.FaviconPath))
    {

            
            #line default
            #line hidden
WriteLiteral("        ");

WriteLiteral(" <link rel=\"shortcut icon\" href=\"");


            
            #line 32 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                      Write(DashboardOptions.FaviconPath);

            
            #line default
            #line hidden
WriteLiteral("\" type=\"image/x-icon\">\r\n");


            
            #line 33 "..\..\Dashboard\Pages\LayoutPage.cshtml"
    }
    else
    {

            
            #line default
            #line hidden
WriteLiteral("        ");

WriteLiteral(" <link rel=\"shortcut icon\" href=\"data:image/x-icon;,\" type=\"image/x-icon\">\r\n");


            
            #line 37 "..\..\Dashboard\Pages\LayoutPage.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral("    \r\n    <link rel=\"stylesheet\" href=\"");


            
            #line 39 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                            Write(Url.To($"/css{version.Major}{version.Minor}{version.Build}0{Math.Abs(DashboardRoutes.StylesheetsHashCode)}"));

            
            #line default
            #line hidden
WriteLiteral("\">\r\n");


            
            #line 40 "..\..\Dashboard\Pages\LayoutPage.cshtml"
     if (DashboardOptions.DarkModeEnabled)
    {

            
            #line default
            #line hidden
WriteLiteral("        ");

WriteLiteral(" <link rel=\"stylesheet\" href=\"");


            
            #line 42 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                   Write(Url.To($"/css-dark{version.Major}{version.Minor}{version.Build}0{Math.Abs(DashboardRoutes.StylesheetsDarkModeHashCode)}"));

            
            #line default
            #line hidden
WriteLiteral("\">\r\n");


            
            #line 43 "..\..\Dashboard\Pages\LayoutPage.cshtml"
    }

            
            #line default
            #line hidden
WriteLiteral(@"</head>
    <body>
        <!-- Wrap all page content here -->
        <div id=""wrap"">

            <!-- Fixed navbar -->
            <div class=""navbar navbar-default navbar-fixed-top"">
                <div class=""container"">
                    <div class=""navbar-header"">
                        <button type=""button"" class=""navbar-toggle"" data-toggle=""collapse"" data-target="".navbar-collapse"">
                            <span class=""icon-bar""></span>
                            <span class=""icon-bar""></span>
                            <span class=""icon-bar""></span>
                        </button>
                        <a class=""navbar-brand"" href=""");


            
            #line 58 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                                 Write(Url.Home());

            
            #line default
            #line hidden
WriteLiteral("\">");


            
            #line 58 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                                              Write(Html.Raw(DashboardOptions.DashboardTitle));

            
            #line default
            #line hidden
WriteLiteral("</a>\r\n                    </div>\r\n                    <div class=\"collapse navbar" +
"-collapse\">\r\n                        ");


            
            #line 61 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                   Write(Html.RenderPartial(new Navigation()));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 62 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                         if(@AppPath != null) {

            
            #line default
            #line hidden
WriteLiteral("                            <ul class=\"nav navbar-nav navbar-right\">\r\n           " +
"                     <li>\r\n                                    <a href=\"");


            
            #line 65 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                        Write(AppPath);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                        <span class=\"glyphicon glyphicon-log-" +
"out\"></span>\r\n                                        <span class=\"hidden-sm\">\r\n" +
"                                            ");


            
            #line 68 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                       Write(Strings.LayoutPage_Back);

            
            #line default
            #line hidden
WriteLiteral("\r\n                                        </span>\r\n                              " +
"      </a>\r\n                                </li>\r\n                            <" +
"/ul>\r\n");


            
            #line 73 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </div>\r\n                    <!--/.nav-collapse -->\r\n         " +
"       </div>\r\n                <!-- Error alert when polling fails -->\r\n        " +
"        ");


            
            #line 78 "..\..\Dashboard\Pages\LayoutPage.cshtml"
           Write(Html.RenderPartial(new ErrorAlert()));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n\r\n            <!-- Begin page content -->\r\n            <div" +
" class=\"container js-page-container margin-bottom-20p\">\r\n                ");


            
            #line 83 "..\..\Dashboard\Pages\LayoutPage.cshtml"
           Write(RenderBody());

            
            #line default
            #line hidden
WriteLiteral(@"
            </div>
        </div>

        <div id=""footer"">
            <div class=""container"">
                <ul class=""list-inline credit"">
                    <li>
                        <a href=""https://www.hangfire.io/"" target=""_blank"" rel=""noopener noreferrer"">Hangfire ");


            
            #line 91 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                                                                                          Write($"{version.Major}.{version.Minor}.{version.Build}");

            
            #line default
            #line hidden
WriteLiteral("\r\n                        </a>\r\n                    </li>\r\n");


            
            #line 94 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                     if(DashboardOptions.DisplayStorageConnectionString){

            
            #line default
            #line hidden
WriteLiteral("                    <li>");


            
            #line 95 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                   Write(Storage);

            
            #line default
            #line hidden
WriteLiteral("</li>\r\n");


            
            #line 96 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                    }

            
            #line default
            #line hidden

            
            #line 97 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                     if (StorageUtcNow.HasValue)
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <li>");


            
            #line 99 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                       Write(Strings.LayoutPage_Footer_StorageTime);

            
            #line default
            #line hidden
WriteLiteral(" ");


            
            #line 99 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                                              Write(Html.LocalTime(StorageUtcNow.Value));

            
            #line default
            #line hidden
WriteLiteral("</li>\r\n");


            
            #line 100 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                    <li>\r\n");


            
            #line 102 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                         if (TimeDifference.HasValue && Math.Abs(TimeDifference.Value.TotalSeconds) > 30)
                        {

            
            #line default
            #line hidden
WriteLiteral("                            <span class=\"text-warning\" title=\"");


            
            #line 104 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                                         Write(Strings.LayoutPage_Footer_TimeIsOutOfSync);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n                                <span class=\"glyphicon glyphicon-warning-sign" +
"\"></span>&nbsp;");


            
            #line 105 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                                                                       Write(Strings.LayoutPage_Footer_Time);

            
            #line default
            #line hidden
WriteLiteral(" ");


            
            #line 105 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                                                                                                       Write(Html.LocalTime(ApplicationUtcNow));

            
            #line default
            #line hidden
WriteLiteral("\r\n                            </span>\r\n");


            
            #line 107 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                        }
                        else
                        {
                            
            
            #line default
            #line hidden
            
            #line 110 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                       Write(Strings.LayoutPage_Footer_Time);

            
            #line default
            #line hidden
            
            #line 110 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                                            
            
            #line default
            #line hidden
            
            #line 110 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                                       Write(Html.LocalTime(ApplicationUtcNow));

            
            #line default
            #line hidden
            
            #line 110 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                                                                                              
                        }

            
            #line default
            #line hidden
WriteLiteral("                    </li>\r\n                    <li>");


            
            #line 113 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                   Write(String.Format(Strings.LayoutPage_Footer_Generatedms, GenerationTime.Elapsed.TotalMilliseconds.ToString("N")));

            
            #line default
            #line hidden
WriteLiteral("</li>\r\n                </ul>\r\n            </div>\r\n        </div>\r\n        \r\n     " +
"   <div id=\"hangfireConfig\"\r\n             data-pollinterval=\"");


            
            #line 119 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                           Write(DashboardOptions.StatsPollingInterval);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n             data-pollurl=\"");


            
            #line 120 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                       Write(Url.To("/stats"));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n             data-darkmode=\"");


            
            #line 121 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                        Write(DashboardOptions.DarkModeEnabled.ToString().ToLowerInvariant());

            
            #line default
            #line hidden
WriteLiteral("\">\r\n        </div>\r\n\r\n        <script src=\"");


            
            #line 124 "..\..\Dashboard\Pages\LayoutPage.cshtml"
                Write(Url.To($"/js{version.Major}{version.Minor}{version.Build}0{Math.Abs(DashboardRoutes.JavaScriptsHashCode)}"));

            
            #line default
            #line hidden
WriteLiteral("\"></script>\r\n    </body>\r\n</html>\r\n");


        }
    }
}
#pragma warning restore 1591
