using System;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests
{
    public class BackgroundJobClientExtensionsFacts
    {
        private const string JobId = "job-id";

        private readonly Mock<IBackgroundJobClient> _client;
        private readonly Mock<IState> _state;

        public BackgroundJobClientExtensionsFacts()
        {
            _client = new Mock<IBackgroundJobClient>();
            _state = new Mock<IState>();

            var job = Job.FromExpression<TestJob>(x => x.TestMethod());

            var jobData = new JobData { Job = job };

            var connection = new Mock<IStorageConnection>();
            connection.Setup(x => x.GetJobData(JobId)).Returns(jobData);

            var storage = new Mock<JobStorage>();
            storage.Setup(x => x.GetConnection()).Returns(connection.Object);

            JobStorage.Current = storage.Object;
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
                It.Is<ScheduledState>(state => state.EnqueueAt > DateTime.UtcNow
                                               && state.QueueName == EnqueuedState.DefaultQueue)));
        }

        [Fact]
        public void StaticSchedule_ShouldCreateAJobInTheScheduledState_InTheCorrectQueue()
        {
            _client.Object.Schedule(() => StaticMethod(), TimeSpan.FromDays(1), "new_queue");

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.EnqueueAt > DateTime.UtcNow
                                               && state.QueueName == "new_queue")));
        }

        [Fact]
        public void StaticSchedule_WithDateTimeOffset_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Schedule(
                    null, () => StaticMethod(), DateTimeOffset.UtcNow));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void StaticSchedule_WithDateTimeOffset_ShouldCreateAJob_InTheScheduledState()
        {
            var now = DateTimeOffset.Now;

            _client.Object.Schedule(() => StaticMethod(), now);

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.EnqueueAt == now.UtcDateTime)));
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
                It.Is<ScheduledState>(
                    state => state.EnqueueAt > DateTime.UtcNow && state.QueueName == EnqueuedState.DefaultQueue)));
        }

        [Fact]
        public void InstanceSchedule_ShouldCreateAJobInTheScheduledState_InTheCorrectQueue()
        {
            _client.Object.Schedule<BackgroundJobClientExtensionsFacts>(
                x => x.InstanceMethod(), TimeSpan.FromDays(1), "new_queue");

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.EnqueueAt > DateTime.UtcNow
                                               && state.QueueName == "new_queue")));
        }

        [Fact]
        public void InstanceSchedule_WithDateTimeOffset_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Schedule<BackgroundJobClientExtensionsFacts>(
                    null, x => x.InstanceMethod(), DateTimeOffset.UtcNow));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void InstanceSchedule_WithDateTimeOffset_ShouldCreateAJobInTheScheduledState()
        {
            var now = DateTimeOffset.Now;

            _client.Object.Schedule<BackgroundJobClientExtensionsFacts>(
                x => x.InstanceMethod(),
                now);

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.EnqueueAt == now.UtcDateTime)));
        }

        [Fact]
        public void ChangeState_WithoutFromState_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.ChangeState(null, "job-id", _state.Object));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void ChangeState_WithoutFromState_CallsItsOverload()
        {
            _client.Object.ChangeState("job-id", _state.Object);

            _client.Verify(x => x.ChangeState("job-id", _state.Object, null));
        }

        [Fact]
        public void Delete_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Delete(null, JobId));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void Delete_ChangesTheStateOfAJob_ToDeleted()
        {
            _client.Object.Delete(JobId);

            _client.Verify(x => x.ChangeState(
                JobId,
                It.IsAny<DeletedState>(),
                null));
        }

        [Fact]
        public void Delete_WithFromState_ChangesTheStateOfAJob_ToDeletedWithFromStateValue()
        {
            _client.Object.Delete(JobId, FailedState.StateName);

            _client.Verify(x => x.ChangeState(
                JobId,
                It.IsAny<DeletedState>(),
                FailedState.StateName));
        }

        [Fact]
        public void Requeue_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Requeue(null, JobId, FailedState.StateName));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void Requeue_ChangesTheStateOfAJob_ToEnqueued_InDefaultQueue()
        {
            _client.Object.Requeue(JobId);

            _client.Verify(
                x =>
                    x.ChangeState(JobId, It.Is<EnqueuedState>(state => state.Queue == EnqueuedState.DefaultQueue),
                        null));
        }

        [Fact]
        public void Requeue_WithFromState_ChangesTheStateOfAJob_ToEnqueued_InDefaultQueue_FromTheGivenState()
        {
            _client.Object.Requeue(JobId, FailedState.StateName);

            _client.Verify(
                x =>
                    x.ChangeState(JobId, It.Is<EnqueuedState>(state => state.Queue == EnqueuedState.DefaultQueue),
                        FailedState.StateName));
        }

        [Fact]
        public void Requeue_ChangesTheStateOfAJob_ToEnqueued_InCustomQueue()
        {
            var customQueueName = "custom_queue";
            _client.Object.Requeue(JobId, null, customQueueName);

            _client.Verify(
                x =>
                    x.ChangeState(JobId, It.Is<EnqueuedState>(state => state.Queue == customQueueName),
                        null));
        }

        [Fact]
        public void Requeue_WithFromState_ChangesTheStateOfAJob_ToEnqueued_InCustomQueue_FromTheGivenState()
        {
            var customQueueName = "custom_queue";
            _client.Object.Requeue(JobId, FailedState.StateName, customQueueName);

            _client.Verify(
                x =>
                    x.ChangeState(JobId, It.Is<EnqueuedState>(state => state.Queue == customQueueName),
                        FailedState.StateName));
        }

        public static void StaticMethod()
        {
        }

        public void InstanceMethod()
        {
        }

        public class TestJob
        {
            public void TestMethod() { }
        }
    }
}
