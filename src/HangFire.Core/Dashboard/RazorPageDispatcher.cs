using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HangFire.Dashboard.Pages;
using Microsoft.Owin;

namespace HangFire.Dashboard
{
    internal class RazorPageDispatcher : IRequestDispatcher
    {
        private readonly Func<Match, RazorPage> _pageFunc;

        public RazorPageDispatcher(Func<Match, RazorPage> pageFunc)
        {
            _pageFunc = pageFunc;
        }

        public Task Dispatch(IOwinContext context, Match match)
        {
            var page = _pageFunc(match);
            page.Request = context.Request;
            page.Response = context.Response;
            
            return context.Response.WriteAsync(page.TransformText());
        }
    }
}
