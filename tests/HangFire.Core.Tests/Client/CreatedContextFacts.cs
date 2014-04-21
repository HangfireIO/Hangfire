using System;
using HangFire.Client;
using HangFire.Client.Filters;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Client
{
    public class CreatedContextFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_WhenCreateContextIsNull()
        {
            Assert.Throws<NullReferenceException>(
                () => new CreatedContext(null, false, null));
        }

        [Fact]
        public void Ctor_CorrectlySetsAllProperties()
        {
            var connection = new Mock<IStorageConnection>();
            var job = Job.FromExpression(() => TestMethod());
            var state = new Mock<State>();
            var exception = new Exception();

            var createContext = new CreateContext(connection.Object, job, state.Object);
            var context = new CreatedContext(createContext, true, exception);

            Assert.True(context.Canceled);
            Assert.Same(exception, context.Exception);
        }

        public static void TestMethod() { }
    }
}
