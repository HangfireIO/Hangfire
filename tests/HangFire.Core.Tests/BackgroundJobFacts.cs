using System;
using HangFire.Common;
using HangFire.States;
using Moq;
using Xunit;

namespace HangFire.Core.Tests
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
            _client.Verify(x => x.Dispose());
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void EnqueueGeneric_CreatesAJobInEnqueuedState()
        {
            Initialize();

            BackgroundJob.Enqueue<BackgroundJobFacts>(x => x.Method());

            _client.Verify(x => x.Create(It.IsNotNull<Job>(), It.IsAny<EnqueuedState>()));
            _client.Verify(x => x.Dispose());
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void Enqueue_WithQueue_CreatesAJobInEnqueuedState_WithCorrespondingQueue()
        {
            Initialize();

            BackgroundJob.Enqueue(() => Method(), "queue");

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(), 
                It.Is<EnqueuedState>(state => state.Queue == "queue")));
            _client.Verify(x => x.Dispose());
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void EnqueueGeneric_WithQueue_CreatesAJobInEnqueuedState_WithCorrespondingQueue()
        {
            Initialize();

            BackgroundJob.Enqueue<BackgroundJobFacts>(x => Method(), "queue");

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<EnqueuedState>(state => state.Queue == "queue")));
            _client.Verify(x => x.Dispose());
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void Schedule_WithTimeSpan_CreatesAJobInScheduledState()
        {
            Initialize();

            BackgroundJob.Schedule(() => Method(), TimeSpan.FromDays(1));

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.EnqueueAt > DateTime.UtcNow)));
            _client.Verify(x => x.Dispose());
        }

        [Fact, GlobalLock(Reason = "Access BackgroundJob.ClientFactory member")]
        public void ScheduleGeneric_WithTimeSpan_CreatesAJobInScheduledState()
        {
            Initialize();

            BackgroundJob.Schedule<BackgroundJobFacts>(x => Method(), TimeSpan.FromDays(1));

            _client.Verify(x => x.Create(
                It.IsNotNull<Job>(),
                It.Is<ScheduledState>(state => state.EnqueueAt > DateTime.UtcNow)));
            _client.Verify(x => x.Dispose());
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
