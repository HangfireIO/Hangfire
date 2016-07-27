using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Server
{
    public class BackgroundProcessingServerFacts
    {
        private readonly string[] _queues = { "queue" };
        private readonly Mock<JobStorage> _storage;
        private readonly List<IBackgroundProcess> _processes;
        private readonly Dictionary<string, object> _properties;
        private readonly Mock<IStorageConnection> _connection;

        public BackgroundProcessingServerFacts()
        {
            _storage = new Mock<JobStorage>();
            _processes = new List<IBackgroundProcess>();
            _properties = new Dictionary<string, object> { { "Queues", _queues } };

            _connection = new Mock<IStorageConnection>();
            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundProcessingServer(null, _processes, _properties));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProcessesArgumentIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundProcessingServer(_storage.Object, null, _properties));

            Assert.Equal("processes", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenPropertiesArgumentIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundProcessingServer(_storage.Object, _processes, null));
            
            Assert.Equal("properties", exception.ParamName);
        }

        [Fact]
        public void Ctor_AnnouncesTheServer_AndRemovesIt()
        {
            using (CreateServer()) { Thread.Sleep(50); }

            _connection.Verify(x => x.AnnounceServer(
                It.IsNotNull<string>(),
                It.Is<ServerContext>(y => y.Queues == _queues)));

            _connection.Verify(x => x.RemoveServer(It.IsNotNull<string>()));
        }

        [Fact]
        public void Execute_StartsAllTheProcesses_InLoop_AndWaitsForThem()
        {
            // Arrange
            var component1Countdown = new CountdownEvent(5);
            var component2Countdown = new CountdownEvent(5);

            var component1 = CreateProcessMock<IBackgroundProcess>();
            component1.Setup(x => x.Execute(It.IsAny<BackgroundProcessContext>())).Callback(() =>
            {
                component1Countdown.Signal();
            });

            var component2 = CreateProcessMock<IBackgroundProcess>();
            component2.Setup(x => x.Execute(It.IsAny<BackgroundProcessContext>())).Callback(() =>
            {
                component2Countdown.Signal();
            });

            // Act
            using (CreateServer())
            {
                WaitHandle.WaitAll(new[] { component1Countdown.WaitHandle, component2Countdown.WaitHandle });
            }

            // Assert
            component1.Verify(x => x.Execute(It.IsAny<BackgroundProcessContext>()), Times.AtLeast(5));
            component2.Verify(x => x.Execute(It.IsNotNull<BackgroundProcessContext>()), Times.AtLeast(5));
        }

        private BackgroundProcessingServer CreateServer()
        {
            return new BackgroundProcessingServer(_storage.Object, _processes, _properties);
        }

        private Mock<T> CreateProcessMock<T>()
            where T : class, IBackgroundProcess
        {
            var mock = new Mock<T>();
            _processes.Add(mock.Object);

            return mock;
        }
    }
}
