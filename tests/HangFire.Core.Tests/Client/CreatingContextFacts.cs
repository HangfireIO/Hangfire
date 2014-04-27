using System;
using HangFire.Client;
using HangFire.Client.Filters;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Client
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
            var state = new Mock<State>();
            var stateMachineFactory = new Mock<IStateMachineFactory>();

            var createContext = new CreateContext(
                connection.Object, stateMachineFactory.Object, job, state.Object);
            var context = new CreatingContext(createContext);

            Assert.False(context.Canceled);
        }

        public static void TestMethod() { }
    }
}
