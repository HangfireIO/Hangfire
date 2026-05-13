using System;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ServerResourceCommandProcessFacts
    {
        private readonly BackgroundProcessContextMock _context;
        private readonly Mock<JobStorageConnection> _connection;
        private readonly JobServerResource _resource;

        public ServerResourceCommandProcessFacts()
        {
            _context = new BackgroundProcessContextMock();
            _context.Properties["Queues"] = new[] { "default", "critical" };
            _context.Properties["WorkerCount"] = 2;

            _resource = new JobServerResource();
            _context.Properties["Resource"] = _resource;

            _connection = new Mock<JobStorageConnection> { CallBase = true };
            _connection.Setup(x => x.AddServerResourceEvent(It.IsAny<ServerResourceEvent>()));
            _connection.Setup(x => x.ClearServerResourceCommand(It.IsAny<string>(), It.IsAny<string>()));
            _connection.Setup(x => x.UpdateServer(It.IsAny<string>(), It.IsAny<ServerContext>()));
            _context.Storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        [Fact]
        public void Execute_AppliesDrainCommandAndClearsIt()
        {
            var command = new ServerResourceCommand
            {
                CommandId = "command-1",
                Command = "drain",
                Reason = "deployment",
                CreatedBy = "operator@example.com"
            };
            ServerResourceEvent resourceEvent = null;
            ServerContext serverContext = null;

            _connection.Setup(x => x.GetServerResourceCommand(_context.ServerId)).Returns(command);
            _connection.Setup(x => x.AddServerResourceEvent(It.IsAny<ServerResourceEvent>()))
                .Callback<ServerResourceEvent>(x => resourceEvent = x);
            _connection.Setup(x => x.UpdateServer(_context.ServerId, It.IsAny<ServerContext>()))
                .Callback<string, ServerContext>((_, x) => serverContext = x);

            CreateProcess().Execute(_context.Object);

            Assert.False(_resource.CanAllocate());
            Assert.True(_resource.GetSnapshot().DrainMode);
            Assert.Equal(JobServerAllocationState.Draining, _resource.GetSnapshot().AllocationState);
            Assert.Equal("deployment", _resource.GetSnapshot().Reason);
            Assert.Equal("command-observed", resourceEvent.EventType);
            Assert.Equal("operator@example.com", resourceEvent.Source);
            Assert.True(serverContext.DrainMode);
            Assert.False(serverContext.CanAllocate);
            _connection.Verify(x => x.ClearServerResourceCommand(_context.ServerId, "command-1"), Times.Once);
        }

        [Fact]
        public void Execute_AppliesResumeCommand()
        {
            _resource.Drain("deployment");
            _connection.Setup(x => x.GetServerResourceCommand(_context.ServerId)).Returns(new ServerResourceCommand
            {
                CommandId = "command-2",
                Command = "resume"
            });

            CreateProcess().Execute(_context.Object);

            Assert.True(_resource.CanAllocate());
            Assert.False(_resource.GetSnapshot().DrainMode);
            _connection.Verify(x => x.ClearServerResourceCommand(_context.ServerId, "command-2"), Times.Once);
        }

        [Fact]
        public void Execute_AppliesQueueDrainCommandAndPublishesQueueState()
        {
            ServerContext serverContext = null;
            _connection.Setup(x => x.GetServerResourceCommand(_context.ServerId)).Returns(new ServerResourceCommand
            {
                CommandId = "command-3",
                Command = "drain-queue",
                Queue = "critical",
                Reason = "GPU maintenance"
            });
            _connection.Setup(x => x.UpdateServer(_context.ServerId, It.IsAny<ServerContext>()))
                .Callback<string, ServerContext>((_, x) => serverContext = x);

            CreateProcess().Execute(_context.Object);

            Assert.Equal(new[] { "default" }, _resource.GetAvailableQueues(new[] { "default", "critical" }));
            Assert.False(serverContext.QueueAllocation["critical"].CanAllocate);
            Assert.True(serverContext.QueueAllocation["critical"].DrainMode);
            Assert.Equal("GPU maintenance", serverContext.QueueAllocation["critical"].Reason);
        }

        [Fact]
        public void Execute_AppliesQueueResumeCommand()
        {
            _resource.DrainQueue("critical", "GPU maintenance");
            _connection.Setup(x => x.GetServerResourceCommand(_context.ServerId)).Returns(new ServerResourceCommand
            {
                CommandId = "command-4",
                Command = "resume-queue",
                Queue = "critical"
            });

            CreateProcess().Execute(_context.Object);

            Assert.Equal(new[] { "default", "critical" }, _resource.GetAvailableQueues(new[] { "default", "critical" }));
            _connection.Verify(x => x.ClearServerResourceCommand(_context.ServerId, "command-4"), Times.Once);
        }

        [Fact]
        public void Execute_IgnoresUnknownCommandsWithoutChangingResourceState()
        {
            _connection.Setup(x => x.GetServerResourceCommand(_context.ServerId)).Returns(new ServerResourceCommand
            {
                CommandId = "command-5",
                Command = "mystery",
                Reason = "unknown"
            });

            CreateProcess().Execute(_context.Object);

            Assert.True(_resource.CanAllocate());
            Assert.Equal(JobServerAllocationState.Available, _resource.GetSnapshot().AllocationState);
        }

        private ServerResourceCommandProcess CreateProcess()
        {
            return new ServerResourceCommandProcess(_resource, _resource, TimeSpan.FromMilliseconds(1));
        }
    }
}
