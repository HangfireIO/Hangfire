using System;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Xunit;

// ReSharper disable PossibleNullReferenceException

namespace Hangfire.Core.Tests
{
    public class BackgroundJobFacts
    {
        private readonly Mock<IBackgroundJobClient> _client;

        public BackgroundJobFacts()
        {
            _client = new Mock<IBackgroundJobClient>();
        }
        
        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void Enqueue_CreatesAJobInEnqueuedState()
        {
            Initialize();

            BackgroundJob.Enqueue(() => Method());

            _client.Verify(x => x.Create(It.IsNotNull<Job>(), It.IsAny<EnqueuedState>()));
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void EnqueueGeneric_CreatesAJobInEnqueuedState()
        {
            Initialize();

            BackgroundJob.Enqueue<BackgroundJobFacts>(x => x.Method());

            _client.Verify(x => x.Create(It.IsNotNull<Job>(), It.IsAny<EnqueuedState>()));
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void Schedule_WithTimeSpan_CreatesAJobInScheduledState()
        {
            Initialize();

            BackgroundJob.Schedule(() => Method(), TimeSpan.FromDays(1));

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.EnqueueAt > DateTime.UtcNow)));
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void Schedule_WithDateTimeOffset_CreatesAJobInScheduledState()
        {
            Initialize();

            BackgroundJob.Schedule(() => Method(), DateTimeOffset.Now);

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.IsNotNull<ScheduledState>()));
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void ScheduleGeneric_WithTimeSpan_CreatesAJobInScheduledState()
        {
            Initialize();

            BackgroundJob.Schedule<BackgroundJobFacts>(x => Method(), TimeSpan.FromDays(1));

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.EnqueueAt > DateTime.UtcNow)));
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void ScheduleGeneric_WithDateTimeOffset_CreatesAJobInScheduledState()
        {
            Initialize();

            BackgroundJob.Schedule<BackgroundJobFacts>(x => x.Method(), DateTimeOffset.Now);

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.IsNotNull<ScheduledState>()));
        }

        [Fact, GlobalLock]
        public void Delete_ChangesStateOfAJobToDeleted()
        {
            Initialize();

            BackgroundJob.Delete("job-id");

            _client.Verify(x => x.ChangeState(
                "job-id",
                It.IsAny<DeletedState>(),
                null));
        }

        [Fact, GlobalLock]
        public void Delete_WithFromState_ChangesStateOfAJobToDeleted_WithFromState()
        {
            Initialize();

            BackgroundJob.Delete("job-id", FailedState.StateName);

            _client.Verify(x => x.ChangeState(
                "job-id",
                It.IsAny<DeletedState>(),
                FailedState.StateName));
        }

        [Fact, GlobalLock]
        public void Requeue_ChangesStateOfAJobToEnqueued()
        {
            Initialize();

            BackgroundJob.Requeue("job-id");

            _client.Verify(x => x.ChangeState(
                "job-id",
                It.IsAny<EnqueuedState>(),
                null));
        }

        [Fact, GlobalLock]
        public void Requeue_WithFromState_ChangesStateOfAJobToEnqueued_WithFromState()
        {
            Initialize();

            BackgroundJob.Requeue("job-id", FailedState.StateName);

            _client.Verify(x => x.ChangeState(
                "job-id",
                It.IsAny<EnqueuedState>(),
                FailedState.StateName));
        }

        [Fact, GlobalLock]
        public void Reschedule_WithTimeSpan_ChangesStateOfAJobToScheduled()
        {
            Initialize();

            BackgroundJob.Reschedule("job-id", TimeSpan.FromDays(1));

            _client.Verify(x => x.ChangeState(
              "job-id",
              It.Is<ScheduledState>(state => state.EnqueueAt > DateTime.UtcNow),
              null));
        }

        [Fact, GlobalLock]
        public void Reschedule_WithTimeSpan_WithFromState_ChangesStateOfAJobToScheduled_WithFromState()
        {
            Initialize();

            BackgroundJob.Reschedule("job-id", TimeSpan.FromDays(1), FailedState.StateName);

            _client.Verify(x => x.ChangeState(
              "job-id",
              It.Is<ScheduledState>(state => state.EnqueueAt > DateTime.UtcNow),
              FailedState.StateName));
        }

        [Fact, GlobalLock]
        public void Reschedule_WithDateTimeOffset_ChangesStateOfAJobToScheduled()
        {
            var now = DateTimeOffset.Now;

            Initialize();

            BackgroundJob.Reschedule("job-id", now);

            _client.Verify(x => x.ChangeState(
              "job-id",
              It.Is<ScheduledState>(state => state.EnqueueAt == now.UtcDateTime),
              null));
        }

        [Fact, GlobalLock]
        public void Reschedule_WithDateTimeOffset_WithFromState_ChangesStateOfAJobToScheduled_WithFromState()
        {
            var now = DateTimeOffset.Now;

            Initialize();

            BackgroundJob.Reschedule("job-id", now, FailedState.StateName);

            _client.Verify(x => x.ChangeState(
              "job-id",
              It.Is<ScheduledState>(state => state.EnqueueAt == now.UtcDateTime),
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
    }
}
