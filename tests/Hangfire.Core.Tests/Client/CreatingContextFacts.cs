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
            var context = CreateContext();

            Assert.False(context.Canceled);
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

            // Does noto throw
            context.SetJobParameter("name", null);
        }

        [Fact]
        public void SetJobParameter_CanBeCalledTwice_WithTheSameName()
        {
            var context = CreateContext();
            context.SetJobParameter("name", null);

            // Does not throw
            context.SetJobParameter("name", null);
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

        public static void TestMethod() { }

        private CreatingContext CreateContext()
        {
            var storage = new Mock<JobStorage>();
            var connection = new Mock<IStorageConnection>();
            var job = Job.FromExpression(() => TestMethod());
            var state = new Mock<IState>();

            var createContext = new CreateContext(storage.Object, connection.Object, job, state.Object);
            return new CreatingContext(createContext);            
        }
    }
}
