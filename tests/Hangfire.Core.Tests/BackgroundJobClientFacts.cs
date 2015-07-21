using System;
using System.Threading;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class BackgroundJobClientFacts
    {
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IJobCreationProcess> _process;
        private readonly Mock<IState> _state;
        private readonly Job _job;
        private readonly Mock<IStateMachine> _stateMachine;
        private readonly Mock<IStateMachineFactoryFactory> _stateMachineFactoryFactory;

        public BackgroundJobClientFacts()
        {
            _connection = new Mock<IStorageConnection>();
            _storage = new Mock<JobStorage>();
            _storage.Setup(x => x.GetConnection()).Returns(_connection.Object);

            _stateMachine = new Mock<IStateMachine>();

            var stateMachineFactory = new Mock<IStateMachineFactory>();
            stateMachineFactory.Setup(x => x.Create(_connection.Object)).Returns(_stateMachine.Object);

            _stateMachineFactoryFactory = new Mock<IStateMachineFactoryFactory>();
            _stateMachineFactoryFactory.Setup(x => x.CreateFactory(_storage.Object))
                .Returns(stateMachineFactory.Object);

            _process = new Mock<IJobCreationProcess>();
            _state = new Mock<IState>();
            _job = Job.FromExpression(() => Method());
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobClient(null, _stateMachineFactoryFactory.Object, _process.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateMachineFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobClient(_storage.Object, null, _process.Object));

            Assert.Equal("stateMachineFactoryFactory", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenCreationProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobClient(_storage.Object, _stateMachineFactoryFactory.Object, null));

            Assert.Equal("process", exception.ParamName);
        }

        [Fact, GlobalLock(Reason = "Needs JobStorage.Current instance")]
        public void Ctor_UsesCurrent_JobStorageInstance_ByDefault()
        {
            JobStorage.Current = new Mock<JobStorage>().Object;
            // ReSharper disable once ObjectCreationAsStatement
            Assert.DoesNotThrow(() => new BackgroundJobClient());
        }

        [Fact]
        public void Ctor_HasDefaultValue_ForStateMachineFactory()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Assert.DoesNotThrow(() => new BackgroundJobClient(_storage.Object));
        }

        [Fact]
        public void Ctor_HasDefaultValue_ForCreationProcess()
        {
            Assert.DoesNotThrow(
                // ReSharper disable once ObjectCreationAsStatement
                () => new BackgroundJobClient(_storage.Object, _stateMachineFactoryFactory.Object));
        }

        [Fact]
        public void CreateJob_ThrowsAnException_WhenJobIsNull()
        {
            var client = CreateClient();

            var exception = Assert.Throws<ArgumentNullException>(
                () => client.Create(null, _state.Object));

            Assert.Equal("job", exception.ParamName);
        }

        [Fact]
        public void CreateJob_ThrowsAnException_WhenStateIsNull()
        {
            var client = CreateClient();

            var exception = Assert.Throws<ArgumentNullException>(
                () => client.Create(_job, null));

            Assert.Equal("state", exception.ParamName);
        }

        [Fact]
        public void CreateJob_RunsTheJobCreationProcess()
        {
            var client = CreateClient();

            client.Create(_job, _state.Object);

            _process.Verify(x => x.Run(It.IsNotNull<CreateContext>(), It.IsNotNull<IStateMachine>()));
        }

        [Fact]
        public void CreateJob_ReturnsJobIdentifier()
        {
            _process.Setup(x => x.Run(It.IsAny<CreateContext>(), It.IsAny<IStateMachine>())).Returns("some-job");
            var client = CreateClient();

            var id = client.Create(_job, _state.Object);

            Assert.Equal("some-job", id);
        }

        [Fact]
        public void CreateJob_WrapsProcessException_IntoItsOwnException()
        {
            var client = CreateClient();
            _process.Setup(x => x.Run(It.IsAny<CreateContext>(), It.IsAny<IStateMachine>()))
                .Throws<InvalidOperationException>();

            var exception = Assert.Throws<CreateJobFailedException>(
                () => client.Create(_job, _state.Object));

            Assert.NotNull(exception.InnerException);
            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }

        [Fact]
        public void ChangeState_ThrowsAnException_WhenJobIdIsNull()
        {
            var client = CreateClient();

            var exception = Assert.Throws<ArgumentNullException>(
                () => client.ChangeState(null, _state.Object, null));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void ChangeState_ThrowsAnException_WhenStateIsNull()
        {
            var client = CreateClient();

            var exception = Assert.Throws<ArgumentNullException>(
                () => client.ChangeState("jobId", null, null));

            Assert.Equal("state", exception.ParamName);
        }

        [Fact]
        public void ChangeState_ChangesTheStateOfAJob_ToTheGivenOne()
        {
            var client = CreateClient();

            client.ChangeState("job-id", _state.Object, null);

            _stateMachine.Verify(x => x.ChangeState(
                "job-id",
                _state.Object,
                null,
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public void ChangeState_WithFromState_ChangesTheStateOfAJob_WithFromStateValue()
        {
            var client = CreateClient();

            client.ChangeState("job-id", _state.Object, "State");

            _stateMachine.Verify(x => x.ChangeState(
                "job-id",
                _state.Object,
                new[] { "State" },
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public void ChangeState_ReturnsTheResult_OfStateMachineInvocation()
        {
            _stateMachine.Setup(x => x.ChangeState("job-id", _state.Object, null, It.IsAny<CancellationToken>()))
                .Returns(true);
            var client = CreateClient();

            var result = client.ChangeState("job-id", _state.Object, null);

            Assert.True(result);
        }

        public static void Method()
        {
        }

        private BackgroundJobClient CreateClient()
        {
            return new BackgroundJobClient(_storage.Object, _stateMachineFactoryFactory.Object, _process.Object);
        }
    }
}
