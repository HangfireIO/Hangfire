using System;
using Hangfire.Server;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class WorkerContextFacts
    {
        private readonly SharedWorkerContextMock _sharedContext;
        private const int WorkerNumber = 1;

        public WorkerContextFacts()
        {
            _sharedContext = new SharedWorkerContextMock();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenSharedContextNull()
        {
            Assert.Throws<NullReferenceException>(
                () => new WorkerContext(null, WorkerNumber));
        }

        [Fact]
        public void Ctor_CorrectlySetsAllInstanceProperties()
        {
            var context = CreateContext();

            Assert.Equal(WorkerNumber, context.WorkerNumber);
        }
        
        [Fact]
        public void CopyCtor_ThrowsAnException_WhenContextIsNull()
        {
            Assert.Throws<NullReferenceException>(
                () => new WorkerContext(null));
        }

        [Fact]
        public void CopyCtor_CorrectlyCopies_AllPropertyValues()
        {
            var context = CreateContext();
            var contextCopy = new WorkerContext(context);

            Assert.Equal(context.WorkerNumber, contextCopy.WorkerNumber);
        }

        private WorkerContext CreateContext()
        {
            return new WorkerContext(_sharedContext.Object, WorkerNumber);
        }
    }
}
