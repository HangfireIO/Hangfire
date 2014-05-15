using System;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.States
{
    public class StateContextFacts
    {
        private const string JobId = "job";

        private readonly Job _job;
        private readonly Mock<IStorageConnection> _connection;

        public StateContextFacts()
        {
            _job = Job.FromExpression(() => Console.WriteLine());
            _connection = new Mock<IStorageConnection>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateContext(null, _job, _connection.Object));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsEmpty()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateContext(String.Empty, _job, _connection.Object));

            Assert.Equal("jobId", exception.ParamName);
        }
        
        [Fact]
        public void Ctor_DoesNotThrowAnException_WhenJobIsNull()
        {
            Assert.DoesNotThrow(() => new StateContext(JobId, null, _connection.Object));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateContext(JobId, _job, null));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySetsAllProperties()
        {
            var context = CreateContext();

            Assert.Equal(JobId, context.JobId);
            Assert.Same(_job, context.Job);
            Assert.Same(_connection.Object, context.Connection);
        }

        [Fact]
        public void CopyCtor_CopiesAllProperties()
        {
            var context = CreateContext();
            var contextCopy = new StateContext(context);

            Assert.Equal(context.JobId, contextCopy.JobId);
            Assert.Same(context.Job, contextCopy.Job);
            Assert.Same(context.Connection, contextCopy.Connection);
        }

        private StateContext CreateContext()
        {
            return new StateContext(JobId, _job, _connection.Object);
        }
    }
}
