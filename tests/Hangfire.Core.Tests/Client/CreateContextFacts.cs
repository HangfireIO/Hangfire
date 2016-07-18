using System;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable ObjectCreationAsStatement
// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Client
{
    public class CreateContextFacts
    {
        private readonly Job _job;
        private readonly Mock<IState> _state;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<JobStorage> _storage;

        public CreateContextFacts()
        {
            _job = Job.FromExpression(() => Method());
            _state = new Mock<IState>();
            _connection = new Mock<IStorageConnection>();
            _storage = new Mock<JobStorage>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new CreateContext(null, _connection.Object, _job, _state.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new CreateContext(_storage.Object, null, _job, _state.Object));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new CreateContext(_storage.Object, _connection.Object, null, _state.Object));

            Assert.Equal("job", exception.ParamName);
        }

        [Fact]
        public void Ctor_DoesNotThrowAnException_WhenStateIsNull()
        {
            // Does not throw
            new CreateContext(_storage.Object, _connection.Object, _job, null);
        }

        [Fact]
        public void Ctor_CorrectlyInitializes_AllProperties()
        {
            var context = CreateContext();

            Assert.Same(_storage.Object, context.Storage);
            Assert.Same(_connection.Object, context.Connection);
            Assert.Same(_job, context.Job);
            Assert.Same(_state.Object, context.InitialState);

            Assert.NotNull(context.Items);
            Assert.NotNull(context.Parameters);
        }

        [Fact]
        public void CopyCtor_CopiesItemsDictionary_FromTheGivenContext()
        {
            var context = CreateContext();
            var contextCopy = new CreateContext(context);

            Assert.Same(context.Items, contextCopy.Items);
            Assert.Same(context.Parameters, contextCopy.Parameters);
        }

        public static void Method()
        {
        }

        private CreateContext CreateContext()
        {
            return new CreateContext(_storage.Object, _connection.Object, _job, _state.Object);
        }
    }
}
