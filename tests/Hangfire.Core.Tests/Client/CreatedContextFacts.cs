using System;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

#pragma warning disable 618

namespace Hangfire.Core.Tests.Client
{
    public class CreatedContextFacts
    {
        private readonly Exception _exception;
        private readonly BackgroundJobMock _backgroundJob;

        public CreatedContextFacts()
        {
            _exception = new Exception();
            _backgroundJob = new BackgroundJobMock();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenCreateContextIsNull()
        {
            Assert.Throws<NullReferenceException>(
                () => new CreatedContext(null, _backgroundJob.Object, false, null));
        }

        [Fact]
        public void Ctor_CorrectlySetsAllProperties()
        {
            var context = CreateContext();

            Assert.True(context.Canceled);
            Assert.Same(_exception, context.Exception);
            Assert.Equal(_backgroundJob.Id, context.JobId);
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
        public void SetJobParameter_ThrowsAnException_AfterCreateJobWasCalled()
        {
            // TODO: incorrect test.

            var context = CreateContext();

            Assert.Throws<InvalidOperationException>(
                () => context.SetJobParameter("name", "value"));
        }

        public static void TestMethod() { }

        private CreatedContext CreateContext()
        {
            var storage = new Mock<JobStorage>();
            var connection = new Mock<IStorageConnection>();
            var job = Job.FromExpression(() => TestMethod());
            var state = new Mock<IState>();
            
            var createContext = new CreateContext(storage.Object, connection.Object, job, state.Object);
            return new CreatedContext(createContext, _backgroundJob.Object, true, _exception);
        }
    }
}
