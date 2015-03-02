﻿using System;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Client
{
    public class CreatedContextFacts
    {
        private const string JobId = "some-job";
        private readonly Exception _exception;
        
        public CreatedContextFacts()
        {
            _exception = new Exception();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenCreateContextIsNull()
        {
            Assert.Throws<NullReferenceException>(
                () => new CreatedContext(null, JobId, false, null));
        }

        [Fact]
        public void Ctor_CorrectlySetsAllProperties()
        {
            var context = CreateContext();

            Assert.True(context.Canceled);
            Assert.Same(_exception, context.Exception);
            Assert.Equal(JobId, context.JobId);
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
            var connection = new Mock<IStorageConnection>();
            var job = Job.FromExpression(() => TestMethod());
            var state = new Mock<IState>();
            
            var createContext = new CreateContext(connection.Object, job, state.Object);
            return new CreatedContext(createContext, JobId, true, _exception);
        }
    }
}
