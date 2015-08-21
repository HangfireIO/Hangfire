using System;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class StateContextFacts
    {
        private const string JobId = "job";

        private readonly Job _job;
        private readonly DateTime _createdAt;
        private readonly Mock<JobStorage> _storage;

        public StateContextFacts()
        {
            _storage = new Mock<JobStorage>();
            _job = Job.FromExpression(() => Console.WriteLine());
            _createdAt = new DateTime(2012, 12, 12);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateContext(null, JobId, _job, _createdAt));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateContext(_storage.Object, null, _job, _createdAt));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsEmpty()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateContext(_storage.Object, String.Empty, _job, _createdAt));

            Assert.Equal("jobId", exception.ParamName);
        }
        
        [Fact]
        public void Ctor_DoesNotThrowAnException_WhenJobIsNull()
        {
            Assert.DoesNotThrow(() => new StateContext(_storage.Object, JobId, null, _createdAt));
        }

        [Fact]
        public void Ctor_CorrectlySetsAllProperties()
        {
            var context = CreateContext();

            Assert.Equal(_storage.Object, context.Storage);
            Assert.Equal(JobId, context.JobId);
            Assert.Equal(_createdAt, context.CreatedAt);
            Assert.Same(_job, context.Job);
        }

        [Fact]
        public void CopyCtor_CopiesAllProperties()
        {
            var context = CreateContext();
            var contextCopy = new StateContext(context);
            
            Assert.Same(context.Storage, contextCopy.Storage);
            Assert.Equal(context.JobId, contextCopy.JobId);
            Assert.Equal(context.CreatedAt, contextCopy.CreatedAt);
            Assert.Same(context.Job, contextCopy.Job);
        }

        private StateContext CreateContext()
        {
            return new StateContext(_storage.Object, JobId, _job, _createdAt);
        }
    }
}
