using System.Collections.Generic;
using HangFire.Client;
using HangFire.Client.Filters;
using HangFire.Filters;
using HangFire.Server;
using HangFire.Server.Filters;
using Xunit;

namespace HangFire.Tests
{
    public class TestExceptionFilter : IClientExceptionFilter, IServerExceptionFilter
    {
        private readonly string _name;
        private readonly IList<string> _results;
        private readonly bool _handlesException;

        public TestExceptionFilter(
            string name, IList<string> results, bool handlesException = false)
        {
            _name = name;
            _results = results;
            _handlesException = handlesException;
        }

        public void OnClientException(ClientExceptionContext filterContext)
        {
            Assert.NotNull(filterContext);

            _results.Add(_name);

            if (_handlesException)
            {
                filterContext.ExceptionHandled = true;
            }
        }

        public void OnServerException(ServerExceptionContext filterContext)
        {
            Assert.NotNull(filterContext);

            _results.Add(_name);

            if (_handlesException)
            {
                filterContext.ExceptionHandled = true;
            }
        }
    }
}
