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

        public Task Dispatch(IOwinContext context, Match match)
        {
            /*var jobIds = request.Form.GetValues("jobs[]");

            if (jobIds == null)
            {
                Response.StatusCode = 422;
                return;
            }

            foreach (var jobId in jobIds)
            {
                _command(jobId);
            }

            Response.StatusCode = (int)HttpStatusCode.NoContent;*/
            return TaskHelper.FromResult(false);
        }
    }
}
