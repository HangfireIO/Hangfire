using System;
using HangFire.Common;
using HangFire.States;
using Moq;
using Xunit;

namespace HangFire.Core.Tests
{
    public class BackgroundJobClientExtensionsFacts
    {
        private readonly Mock<IBackgroundJobClient> _client;
        private readonly Mock<IState> _state;

        public BackgroundJobClientExtensionsFacts()
        {
            _client = new Mock<IBackgroundJobClient>();
            _state = new Mock<IState>();
        }

        [Fact]
        public void StaticCreate_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Create(
                    null, () => StaticMethod(), _state.Object));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void StaticCreate_ShouldCreateAJobInTheGivenState()
        {
            _client.Object.Create(() => StaticMethod(), _state.Object);
            
            _client.Verify(x => x.Create(It.IsNotNull<Job>(), _state.Object));
        }

        [Fact]
        public void InstanceCreate_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Create<BackgroundJobClientExtensionsFacts>(
                    null, x => x.InstanceMethod(), _state.Object));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void InstanceCreate_ShouldCreateAJobInTheGivenState()
        {
            _client.Object.Create<BackgroundJobClientExtensionsFacts>(x => x.InstanceMethod(), _state.Object);

            _client.Verify(x => x.Create(It.IsNotNull<Job>(), _state.Object));
        }

        [Fact]
        public void StaticEnqueue_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Enqueue(
                    null, () => StaticMethod()));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void StaticEnqueue_ShouldCreateAJobInTheEnqueueState()
        {
            _client.Object.Enqueue(() => StaticMethod());

            _client.Verify(x => x.Create(It.IsNotNull<Job>(), It.IsAny<EnqueuedState>()));
        }

        [Fact]
        public void InstanceEnqueue_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Enqueue<BackgroundJobClientExtensionsFacts>(
                    null, x => x.InstanceMethod()));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void InstanceEnqueue_ShouldCreateAJobInTheEnqueuedState()
        {
            _client.Object.Enqueue<BackgroundJobClientExtensionsFacts>(x => x.InstanceMethod());

            _client.Verify(x => x.Create(It.IsNotNull<Job>(), It.IsAny<EnqueuedState>()));
        }

        [Fact]
        public void StaticEnqueue_WithQueue_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Enqueue(
                    null, () => StaticMethod(), "critical"));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void StaticEnqueue_WithQueue_ShouldCreateAJobInTheEnqueuedState()
        {
            _client.Object.Enqueue(() => StaticMethod(), "critical");

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<EnqueuedState>(state => state.Queue == "critical")));
        }

        [Fact]
        public void InstanceEnqueue_WithQueue_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Enqueue<BackgroundJobClientExtensionsFacts>(
                    null, x => x.InstanceMethod(), "critical"));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void InstanceEnqueue_WithQueue_ShouldCreateAJobInTheEnqueuedState()
        {
            _client.Object.Enqueue<BackgroundJobClientExtensionsFacts>(x => x.InstanceMethod(), "critical");

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<EnqueuedState>(state => state.Queue == "critical")));
        }

        [Fact]
        public void StaticSchedule_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Schedule(
                    null, () => StaticMethod(), TimeSpan.FromDays(1)));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void StaticSchedule_ShouldCreateAJobInTheScheduledState()
        {
            _client.Object.Schedule(() => StaticMethod(), TimeSpan.FromDays(1));

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.EnqueueAt > DateTime.UtcNow)));
        }

        [Fact]
        public void InstanceSchedule_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Schedule<BackgroundJobClientExtensionsFacts>(
                    null, x => x.InstanceMethod(), TimeSpan.FromDays(1)));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void InstanceSchedule_ShouldCreateAJobInTheScheduledState()
        {
            _client.Object.Schedule<BackgroundJobClientExtensionsFacts>(
                x => x.InstanceMethod(), TimeSpan.FromDays(1));

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.EnqueueAt > DateTime.UtcNow)));
        }

        public static void StaticMethod()
        {
        }

        public void InstanceMethod()
        {
        }
    }
}
