using System;
using System.Net;
using System.Web;

namespace HangFire.Web
{
    internal class CommandHandler : GenericHandler
    {
        private readonly Func<bool> _command;

        public CommandHandler(Func<bool> command)
        {
            _command = command;
        }

        public override void ProcessRequest()
        {
            if (Request.HttpMethod != WebRequestMethods.Http.Post)
            {
                throw new HttpException((int)HttpStatusCode.MethodNotAllowed, "Wrong HTTP method.");
            }

            if (_command())
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
