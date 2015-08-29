using System;
using Hangfire.Server;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class WorkerContextFacts
    {
        private static readonly string[] Queues = { "critical", "default" };
        private const string WorkerId = "1";

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueuesArrayIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new WorkerContext(null, WorkerId));

            Assert.Equal("queues", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySetsAllInstanceProperties()
        {
            var context = CreateContext();

            Assert.Equal(Queues, context.Queues);
            Assert.Equal(WorkerId, context.WorkerId);
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

            Assert.Equal(Queues, context.Queues);
            Assert.Equal(context.WorkerId, contextCopy.WorkerId);
        }

        private WorkerContext CreateContext()
        {
            return new WorkerContext(Queues, WorkerId);
        }
    }
}
