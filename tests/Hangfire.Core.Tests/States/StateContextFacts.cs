using System;
using Hangfire.States;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class StateContextFacts
    {
        private readonly Mock<JobStorage> _storage;
        private readonly BackgroundJobMock _backgroundJob;

        public StateContextFacts()
        {
            _storage = new Mock<JobStorage>();
            _backgroundJob = new BackgroundJobMock();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateContext(null, _backgroundJob.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenBackgroundJobIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateContext(_storage.Object, null));

            Assert.Equal("backgroundJob", exception.ParamName);
        }
        
        [Fact]
        public void Ctor_CorrectlySetsAllProperties()
        {
            var context = CreateContext();

            Assert.Equal(_storage.Object, context.Storage);
            Assert.Equal(_backgroundJob.Object, context.BackgroundJob);
        }

        [Fact]
        public void CopyCtor_CopiesAllProperties()
        {
            var context = CreateContext();
            var contextCopy = new StateContext(context);
            
            Assert.Same(context.Storage, contextCopy.Storage);
            Assert.Equal(context.BackgroundJob, contextCopy.BackgroundJob);
        }

        private StateContext CreateContext()
        {
            return new StateContext(_storage.Object, _backgroundJob.Object);
        }
    }
}
