using System;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Client
{
    public class ClientExceptionContextFacts
    {
        private readonly CreateContext _createContext;

        public ClientExceptionContextFacts()
        {
            var storage = new Mock<JobStorage>();
            var connection = new Mock<IStorageConnection>();
            var job = Job.FromExpression(() => TestMethod());
            var state = new Mock<IState>();

            _createContext = new CreateContext(
                storage.Object, connection.Object, job, state.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenCreateContextIsNull()
        {
            Assert.Throws<NullReferenceException>(
                () => new ClientExceptionContext(null, new Exception()));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenExceptionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ClientExceptionContext(_createContext, null));

            Assert.Equal("exception", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllProperties()
        {
            var exception = new Exception();
            var context = new ClientExceptionContext(_createContext, exception);

            Assert.Same(exception, context.Exception);
            Assert.False(context.ExceptionHandled);
        }

        public static void TestMethod()
        {
        }
    }
}
