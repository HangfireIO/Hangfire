using System;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable PossibleNullReferenceException

namespace Hangfire.Core.Tests
{
    public class BackgroundJobFacts
    {
        private const string JobId = "job-id";
        private readonly Mock<IBackgroundJobClient> _client;

        public BackgroundJobFacts()
        {
            _client = new Mock<IBackgroundJobClient>();

            var job = Job.FromExpression<TestJob>(x => x.TestMethod());

            var jobData = new JobData { Job = job };

            var connection = new Mock<IStorageConnection>();
            connection.Setup(x => x.GetJobData(JobId)).Returns(jobData);

            var storage = new Mock<JobStorage>();
            storage.Setup(x => x.GetConnection()).Returns(connection.Object);

            JobStorage.Current = storage.Object;
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void Enqueue_CreatesAJobInEnqueuedState_InCustomQueue()
        {
            Initialize();

            BackgroundJob.Enqueue(() => Method(), "custom_queue");

            _client.Verify(
                x =>
                    x.Create(It.IsNotNull<Job>(),
                        It.Is<EnqueuedState>(state => state.Queue == "custom_queue")));
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void Enqueue_CreatesAJobInEnqueuedState_InDefaultQueue()
        {
            Initialize();

            BackgroundJob.Enqueue(() => Method());

            _client.Verify(
                x =>
                    x.Create(It.IsNotNull<Job>(),
                        It.Is<EnqueuedState>(state => state.Queue == EnqueuedState.DefaultQueue)));
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void EnqueueGeneric_CreatesAJobInEnqueuedState_InDefaultQueue()
        {
            Initialize();

            BackgroundJob.Enqueue<BackgroundJobFacts>(x => x.Method());

            _client.Verify(
                x =>
                    x.Create(It.IsNotNull<Job>(),
                        It.Is<EnqueuedState>(state => state.Queue == EnqueuedState.DefaultQueue)));
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void EnqueueGeneric_CreatesAJobInEnqueuedState_InCustomQueue()
        {
            Initialize();

            BackgroundJob.Enqueue<BackgroundJobFacts>(x => x.Method(), "custom_queue");

            _client.Verify(
                x =>
                    x.Create(It.IsNotNull<Job>(),
                        It.Is<EnqueuedState>(state => state.Queue == "custom_queue")));
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void Schedule_WithTimeSpan_CreatesAJobInScheduledState_InNewQueue()
        {
            Initialize();

            BackgroundJob.Schedule(() => Method(), TimeSpan.FromDays(1), "new_queue");

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.EnqueueAt > DateTime.UtcNow
                                               && state.QueueName == "new_queue")));
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void Schedule_WithDateTimeOffset_CreatesAJobInScheduledState_InNewQueue()
        {
            Initialize();

            BackgroundJob.Schedule(() => Method(), DateTimeOffset.Now, "new_queue");

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.QueueName == "new_queue")));
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void ScheduleGeneric_WithTimeSpan_CreatesAJobInScheduledState_InGenericQueue()
        {
            Initialize();

            BackgroundJob.Schedule<BackgroundJobFacts>(x => Method(), TimeSpan.FromDays(1), "generic_queue");

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.EnqueueAt > DateTime.UtcNow
                                               && state.QueueName == "generic_queue")));
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void ScheduleGeneric_WithDateTimeOffset_CreatesAJobInScheduledState_InGenericQueue()
        {
            Initialize();

            BackgroundJob.Schedule<BackgroundJobFacts>(x => x.Method(), DateTimeOffset.Now, "generic_queue");

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.QueueName == "generic_queue")));
        }

        [Fact, GlobalLock]
        public void Delete_ChangesStateOfAJobToDeleted()
        {
            Initialize();

            BackgroundJob.Delete(JobId);

            _client.Verify(x => x.ChangeState(
                JobId,
                It.IsAny<DeletedState>(),
                null));
        }

        [Fact, GlobalLock]
        public void Delete_WithFromState_ChangesStateOfAJobToDeleted_WithFromState()
        {
            Initialize();

            BackgroundJob.Delete(JobId, FailedState.StateName);

            _client.Verify(x => x.ChangeState(
                JobId,
                It.IsAny<DeletedState>(),
                FailedState.StateName));
        }

        [Fact, GlobalLock]
        public void Requeue_ChangesStateOfAJobToEnqueued_InDefaultQueue()
        {
            Initialize();

            BackgroundJob.Requeue(JobId);

            _client.Verify(x => x.ChangeState(
                JobId,
                It.Is<EnqueuedState>(state => state.Queue == EnqueuedState.DefaultQueue),
                null));
        }

        [Fact, GlobalLock]
        public void Requeue_WithFromState_ChangesStateOfAJobToEnqueued_InCustomQueue_WithFromState()
        {
            Initialize();

            BackgroundJob.Requeue(JobId, FailedState.StateName, "custom_queue");

            _client.Verify(x => x.ChangeState(
                JobId,
                It.Is<EnqueuedState>(state => state.Queue == "custom_queue"),
                FailedState.StateName));
        }

        [Fact, GlobalLock(Reason = "Accesses to BJ.ClientFactory, JS.Current")]
        public void ClientFactory_HasDefaultValue_ThatReturns()
        {
            BackgroundJob.ClientFactory = null;
            JobStorage.Current = new Mock<JobStorage>().Object;

            var client = BackgroundJob.ClientFactory();
            Assert.NotNull(client);
        }

        private void Initialize()
        {
            BackgroundJob.ClientFactory = () => _client.Object;
        }

        public void Method() { }

        public class TestJob
        {
            public void TestMethod() { }
        }
    }
}
