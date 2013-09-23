using System.Net;
using System.Web;

namespace HangFire.Web.Commands
{
    internal class RetryCommand : GenericHandler
    {
        private readonly string _jobId;

        public RetryCommand(string jobId)
        {
            _jobId = jobId;
        }

        public override void ProcessRequest()
        {
            if (Request.HttpMethod != WebRequestMethods.Http.Post)
            {
                throw new HttpException((int)HttpStatusCode.MethodNotAllowed, "Wrong HTTP method.");
            }

            if (JobStorage.RetryJob(_jobId))
            {
                Response.StatusCode = (int)HttpStatusCode.NoContent;
            }
            else
            {
                Response.StatusCode = 422;
            }
        }
    }
}
