using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace HangFire.Dashboard
{
    internal class CommandDispatcher : IRequestDispatcher
    {
        private readonly Func<Match, bool> _command;

        public CommandDispatcher(Func<Match, bool> command)
        {
            _command = command;
        }

        public Task Dispatch(IOwinContext context, Match match)
        {
            if (context.Request.Method != WebRequestMethods.Http.Post)
            {
                context.Response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
                return TaskHelper.FromResult(false);
            }

            if (_command(match))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            }
            else
            {
                context.Response.StatusCode = 422;
            }

            return TaskHelper.FromResult(true);
        }
    }
}
