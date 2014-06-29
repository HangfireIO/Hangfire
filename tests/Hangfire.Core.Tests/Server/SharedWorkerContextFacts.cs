using System;
using Hangfire.Server;
using Hangfire.States;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class SharedWorkerContextFacts
    {
        private const string ServerId = "server";
        private static readonly string[] Queues = { "default" };

        private readonly Mock<IJobPerformanceProcess> _performanceProcess;
        private readonly Mock<JobActivator> _activator;
        private readonly Mock<IStateMachineFactory> _stateMachineFactory;
        private readonly Mock<JobStorage> _storage;

        public SharedWorkerContextFacts()
        {
            _performanceProcess = new Mock<IJobPerformanceProcess>();
            _activator = new Mock<JobActivator>();
            _stateMachineFactory = new Mock<IStateMachineFactory>();
            _storage = new Mock<JobStorage>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SharedWorkerContext(
                    null, Queues, _storage.Object, _performanceProcess.Object, _activator.Object, _stateMachineFactory.Object));

            Assert.Equal("serverId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueuesArrayIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SharedWorkerContext(
                    ServerId, null, _storage.Object, _performanceProcess.Object, _activator.Object, _stateMachineFactory.Object));

            Assert.Equal("queues", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SharedWorkerContext(
                    ServerId, Queues, null, _performanceProcess.Object, _activator.Object, _stateMachineFactory.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenPerformanceProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SharedWorkerContext(
                    ServerId, Queues, _storage.Object, null, _activator.Object, _stateMachineFactory.Object));

            Assert.Equal("performanceProcess", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenActivatorIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SharedWorkerContext(
                    ServerId, Queues, _storage.Object, _performanceProcess.Object, null, _stateMachineFactory.Object));

            Assert.Equal("activator", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SharedWorkerContext(
                    ServerId, Queues, _storage.Object, _performanceProcess.Object, _activator.Object, null));

            Assert.Equal("stateMachineFactory", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlyInitializes_AllProperties()
        {
            var context = CreateContext();

            Assert.Equal(ServerId, context.ServerId);
            Assert.Same(Queues, context.Queues);
            Assert.Same(_storage.Object, context.Storage);
            Assert.Same(_performanceProcess.Object, context.PerformanceProcess);
            Assert.Same(_activator.Object, context.Activator);
            Assert.Same(_stateMachineFactory.Object, context.StateMachineFactory);
        }

        [Fact]
        public void CopyCtor_CorrectlyInitializes_AllProperties()
        {
            var context = CreateContext();
            var contextCopy = new SharedWorkerContext(context);

            Assert.Equal(context.ServerId, contextCopy.ServerId);
            Assert.Same(context.Queues, contextCopy.Queues);
            Assert.Same(context.Storage, contextCopy.Storage);
            Assert.Same(context.PerformanceProcess, contextCopy.PerformanceProcess);
            Assert.Same(context.Activator, contextCopy.Activator);
            Assert.Same(context.StateMachineFactory, contextCopy.StateMachineFactory);
        }

        private SharedWorkerContext CreateContext()
        {
            return new SharedWorkerContext(
                ServerId, Queues, _storage.Object, _performanceProcess.Object, _activator.Object, _stateMachineFactory.Object);
        }
    }
}
