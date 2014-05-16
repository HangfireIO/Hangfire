using System;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests
{
    public class BackgroundJobClientExtensionsFacts
    {
        private const string JobId = "job-id";

        private readonly Mock<IBackgroundJobClient> _client;
        private readonly Mock<IState> _state;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly Mock<IStorageConnection> _connection;

        public BackgroundJobClientExtensionsFacts()
        {
            _client = new Mock<IBackgroundJobClient>();
            _state = new Mock<IState>();

            _stateMachine = new Mock<IStateMachine>();
            _connection = new Mock<IStorageConnection>();

            var factory = new Mock<IStateMachineFactory>();
            factory.Setup(x => x.Create(_connection.Object)).Returns(_stateMachine.Object);

            _client.Setup(x => x.StateMachineFactory).Returns(factory.Object);
            _client.Setup(x => x.Connection).Returns(_connection.Object);
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

        [Fact]
        public void Delete_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Delete(null, JobId));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void Delete_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => _client.Object.Delete(null));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Delete_ChangesTheStateOfAJob_ToDeleted()
        {
            _client.Object.Delete(JobId);

            _stateMachine.Verify(x => x.TryToChangeState(
                JobId,
                It.IsAny<DeletedState>(),
                null));
        }

        [Fact]
        public void Delete_WithFromState_ChangesTheStateOfAJob_ToDeletedWithFromStateValue()
        {
            _client.Object.Delete(JobId, FailedState.StateName);

            _stateMachine.Verify(x => x.TryToChangeState(
                JobId,
                It.IsAny<DeletedState>(),
                new []{ FailedState.StateName }));
        }

        public static void StaticMethod()
        {
        }

        public void InstanceMethod()
        {
        }
    }
}
