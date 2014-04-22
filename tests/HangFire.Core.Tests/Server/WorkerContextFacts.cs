using System;
using HangFire.Server;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class WorkerContextFacts
    {
        private const string Server = "server";
        private static readonly string[] Queues = { "default" };
        private const int WorkerNumber = 1;

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new WorkerContext(null, Queues, WorkerNumber));

            Assert.Equal("serverName", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueuesArgumentIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new WorkerContext(Server, null, WorkerNumber));

            Assert.Equal("queueNames", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySetsAllInstanceProperties()
        {
            var context = CreateContext();

            Assert.Same(Server, context.ServerName);
            Assert.Same(Queues, context.QueueNames);
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

            Assert.Same(context.ServerName, contextCopy.ServerName);
            Assert.Same(context.QueueNames, contextCopy.QueueNames);
            Assert.Equal(context.WorkerNumber, contextCopy.WorkerNumber);
        }

        private static WorkerContext CreateContext()
        {
            return new WorkerContext(Server, Queues, WorkerNumber);
        }
    }
}
