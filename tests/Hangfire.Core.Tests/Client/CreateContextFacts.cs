using System;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Client
{
    public class CreateContextFacts
    {
        private readonly Job _job;
        private readonly Mock<IState> _state;
        private readonly Mock<IStorageConnection> _connection;

        public CreateContextFacts()
        {
            _job = Job.FromExpression(() => Method());
            _state = new Mock<IState>();
            _connection = new Mock<IStorageConnection>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new CreateContext(null, _job, _state.Object));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new CreateContext(_connection.Object, null, _state.Object));

            Assert.Equal("job", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new CreateContext(
                    _connection.Object, _job, null));

            Assert.Equal("initialState", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlyInitializes_AllProperties()
        {
            var context = CreateContext();

            Assert.Same(_connection.Object, context.Connection);
            Assert.Same(_job, context.Job);
            Assert.Same(_state.Object, context.InitialState);

            Assert.NotNull(context.Items);
        }

        [Fact]
        public void CopyCtor_CopiesItemsDictionary_FromTheGivenContext()
        {
            var context = CreateContext();
            var contextCopy = new CreateContext(context);

            Assert.Same(context.Items, contextCopy.Items);
        }

        public static void Method()
        {
        }

        private CreateContext CreateContext()
        {
            return new CreateContext(_connection.Object, _job, _state.Object);
        }
    }
}
