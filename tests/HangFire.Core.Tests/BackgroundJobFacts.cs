using System;
using HangFire.Common;
using HangFire.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests
{
    public class BackgroundJobFacts
    {
        private readonly Mock<IBackgroundJobClient> _client;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly Mock<IStorageConnection> _connection;

        public BackgroundJobFacts()
        {
            _client = new Mock<IBackgroundJobClient>();
            _stateMachine = new Mock<IStateMachine>();
            _connection = new Mock<IStorageConnection>();

            var factory = new Mock<IStateMachineFactory>();
            factory.Setup(x => x.Create(_connection.Object)).Returns(_stateMachine.Object);

            _client.Setup(x => x.StateMachineFactory).Returns(factory.Object);
            _client.Setup(x => x.Connection).Returns(_connection.Object);
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

        [Fact, GlobalLock]
        public void Delete_ChangesStateOfAJobToDeleted()
        {
            Initialize();

            BackgroundJob.Delete("job-id");

            _stateMachine.Verify(x => x.TryToChangeState(
                "job-id",
                It.IsAny<DeletedState>(),
                It.IsAny<string[]>()));
        }

        [Fact, GlobalLock]
        public void Delete_WithFromState_ChangesStateOfAJobToDeleted_WithFromStateArray()
        {
            Initialize();

            BackgroundJob.Delete("job-id", FailedState.StateName);

            _stateMachine.Verify(x => x.TryToChangeState(
                "job-id",
                It.IsAny<DeletedState>(),
                new[] { FailedState.StateName }));
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
