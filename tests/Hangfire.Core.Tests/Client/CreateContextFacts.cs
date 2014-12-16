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

        [Fact]
        public void SetJobParameter_ThrowsAnException_WhenParameterNameIsNull()
        {
            var context = CreateContext();
            var exception = Assert.Throws<ArgumentNullException>(
                () => context.SetJobParameter(null, null));

            Assert.Equal("name", exception.ParamName);
        }

        [Fact]
        public void SetJobParameter_AcceptsNullValues()
        {
            var context = CreateContext();

            Assert.DoesNotThrow(
                () => context.SetJobParameter("name", null));
        }

        [Fact]
        public void SetJobParameter_CanBeCalledTwice_WithTheSameName()
        {
            var context = CreateContext();
            context.SetJobParameter("name", null);

            Assert.DoesNotThrow(
                () => context.SetJobParameter("name", null));
        }

        [Fact]
        public void GetJobParameter_ThrowsAnException_WhenParameterNameIsNull()
        {
            var context = CreateContext();
            Assert.Throws<ArgumentNullException>(
                () => context.GetJobParameter<int>(null));
        }

        [Fact]
        public void GetJobParameter_ReturnsDefaultValue_IfParameterDoesNotExists()
        {
            var context = CreateContext();

            Assert.Equal(default(int), context.GetJobParameter<int>("one"));
            Assert.Equal(default(string), context.GetJobParameter<string>("two"));
        }

        [Fact]
        public void GetJobParameter_ReturnsTheValue_ThatWasSetByTheCorrespondingMethod()
        {
            var context = CreateContext();
            context.SetJobParameter("name", "value");

            Assert.Equal("value", context.GetJobParameter<string>("name"));
        }

        [Fact]
        public void GetJobParameter_ReturnsTheValue_OfTheSpecifiedParameterNameOnly()
        {
            var context = CreateContext();

            context.SetJobParameter("name1", "value1");
            context.SetJobParameter("name2", "value2");

            Assert.Equal("value1", context.GetJobParameter<string>("name1"));
        }

        [Fact]
        public void GetJobParameter_ReturnsTheFreshestValue_WhenTwoSetOperationsPerformed()
        {
            var context = CreateContext();

            context.SetJobParameter("name", "oldValue");
            context.SetJobParameter("name", "newValue");

            Assert.Equal("newValue", context.GetJobParameter<string>("name"));
        }

        [Fact]
        public void GetJobParameter_ThrowsAnException_WhenParameterCouldNotBeDeserialized()
        {
            var context = CreateContext();

            context.SetJobParameter("name", "value");

            Assert.Throws<InvalidOperationException>(
                () => context.GetJobParameter<int>("name"));
        }

        [Fact]
        public void CopyCtor_CopiesJobParameters_FromTheGivenContext()
        {
            var context = CreateContext();
            context.SetJobParameter("name", "value");
            var contextCopy = new CreateContext(context);

            var value = contextCopy.GetJobParameter<string>("name");

            Assert.Equal("value", value);
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
