using System;
using System.Linq;
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
        private readonly Mock<IJobCreationProcess> _creationProcess;
        private readonly Mock<IState> _state;
        private readonly Job _job;
        private readonly Mock<IStateChangeProcess> _stateChangeProcess;

        public BackgroundJobClientFacts()
        {
            var connection = new Mock<IStorageConnection>();
            _storage = new Mock<JobStorage>();
            _storage.Setup(x => x.GetConnection()).Returns(connection.Object);

            _stateChangeProcess = new Mock<IStateChangeProcess>();
            
            _state = new Mock<IState>();
            _state.Setup(x => x.Name).Returns("Mock");
            _job = Job.FromExpression(() => Method());

            _creationProcess = new Mock<IJobCreationProcess>();
            _creationProcess.Setup(x => x.Run(It.IsAny<CreateContext>()))
                .Returns(new BackgroundJob("some-job", _job, DateTime.UtcNow));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobClient(null, _stateChangeProcess.Object, _creationProcess.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateChangeProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobClient(_storage.Object, null, _creationProcess.Object));

            Assert.Equal("stateChangeProcess", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenCreationProcessIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobClient(_storage.Object, _stateChangeProcess.Object, null));

            Assert.Equal("creationProcess", exception.ParamName);
        }

        [Fact, GlobalLock(Reason = "Needs JobStorage.Current instance")]
        public void Ctor_UsesCurrent_JobStorageInstance_ByDefault()
        {
            JobStorage.Current = new Mock<JobStorage>().Object;
            // ReSharper disable once ObjectCreationAsStatement
            Assert.DoesNotThrow(() => new BackgroundJobClient());
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

            _creationProcess.Verify(x => x.Run(It.IsNotNull<CreateContext>()));
        }

        [Fact]
        public void CreateJob_ReturnsJobIdentifier()
        {
            var client = CreateClient();

            var id = client.Create(_job, _state.Object);

            Assert.Equal("some-job", id);
        }

        [Fact]
        public void CreateJob_WrapsProcessException_IntoItsOwnException()
        {
            var client = CreateClient();
            _creationProcess.Setup(x => x.Run(It.IsAny<CreateContext>()))
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

            _stateChangeProcess.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == "job-id" &&
                ctx.NewState == _state.Object &&
                ctx.ExpectedStates == null)));
        }

        [Fact]
        public void ChangeState_WithFromState_ChangesTheStateOfAJob_WithFromStateValue()
        {
            var client = CreateClient();

            client.ChangeState("job-id", _state.Object, "State");

            _stateChangeProcess.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == "job-id" &&
                ctx.NewState == _state.Object &&
                ctx.ExpectedStates.SequenceEqual(new[] { "State" }))));
        }

        [Fact]
        public void ChangeState_ReturnsTheResult_OfStateChangeProcessInvocation()
        {
            _stateChangeProcess.Setup(x => x.ChangeState(It.IsAny<StateChangeContext>()))
                .Returns(_state.Object);
            var client = CreateClient();

            var result = client.ChangeState("job-id", _state.Object, null);

            Assert.True(result);
        }

        public static void Method()
        {
        }

        private BackgroundJobClient CreateClient()
        {
            return new BackgroundJobClient(_storage.Object, _stateChangeProcess.Object, _creationProcess.Object);
        }
    }
}
