using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace HangFire.Dashboard
{
    internal class BatchCommandDispatcher : IRequestDispatcher
    {
        private readonly Action<string> _command;

        public BatchCommandDispatcher(Action<string> command)
        {
            _command = command;
        }

        public async Task Dispatch(IOwinContext context, Match match)
        {
            var form = await context.Request.ReadFormAsync();
            var jobIds = form.GetValues("jobs[]");

            if (jobIds == null)
            {
                context.Response.StatusCode = 422;
                return;
            }

            foreach (var jobId in jobIds)
            {
                _command(jobId);
            }

            context.Response.StatusCode = (int) HttpStatusCode.NoContent;
        }
    }
}
