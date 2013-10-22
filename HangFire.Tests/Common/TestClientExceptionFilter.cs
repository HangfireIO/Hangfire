using System.Collections.Generic;
using HangFire.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HangFire.Tests
{
    public class TestClientExceptionFilter : IClientExceptionFilter
    {
        private readonly string _name;
        private readonly IList<string> _results;
        private readonly bool _handlesException;

        public TestClientExceptionFilter(
            string name, IList<string> results, bool handlesException = false)
        {
            _name = name;
            _results = results;
            _handlesException = handlesException;
        }

        public void OnClientException(ClientExceptionContext filterContext)
        {
            Assert.IsNotNull(filterContext);

            _results.Add(_name);

            if (_handlesException)
            {
                filterContext.ExceptionHandled = true;
            }
        }
    }
}
