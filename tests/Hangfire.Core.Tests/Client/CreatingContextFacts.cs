using System;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Client
{
    public class CreatingContextFacts
    {
        [Fact]
        public void Ctor_ThrowsAnException_WhenContextIsNull()
        {
            Assert.Throws<NullReferenceException>(
                () => new CreatingContext(null));
        }

        [Fact]
        public void Ctor_CanceledProperty_IsFalseByDefault()
        {
            var connection = new Mock<IStorageConnection>();
            var job = Job.FromExpression(() => TestMethod());
            var state = new Mock<IState>();

            var createContext = new CreateContext(connection.Object, job, state.Object);
            var context = new CreatingContext(createContext);

            Assert.False(context.Canceled);
        }

        public static void TestMethod() { }
    }
}
