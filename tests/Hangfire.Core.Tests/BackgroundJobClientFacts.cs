using System;
using System.Linq;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;
#pragma warning disable 618

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests
{
    public class BackgroundJobClientFacts
    {
        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IBackgroundJobFactory> _factory;
        private readonly Mock<IState> _state;
        private readonly Job _job;
        private readonly Mock<IBackgroundJobStateChanger> _stateChanger;

        public BackgroundJobClientFacts()
        {
            var connection = new Mock<IStorageConnection>();
            _storage = new Mock<JobStorage>();
            _storage.Setup(x => x.GetConnection()).Returns(connection.Object);

            _stateChanger = new Mock<IBackgroundJobStateChanger>();
            
            _state = new Mock<IState>();
            _state.Setup(x => x.Name).Returns("Mock");
            _job = Job.FromExpression(() => Method());

            _factory = new Mock<IBackgroundJobFactory>();
            _factory.Setup(x => x.Create(It.IsAny<CreateContext>()))
                .Returns(new BackgroundJob("some-job", _job, DateTime.UtcNow));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobClient(null, _factory.Object, _stateChanger.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenFactoryIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobClient(_storage.Object, null, _stateChanger.Object));

            Assert.Equal("factory", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStateChangerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobClient(_storage.Object, _factory.Object, null));

            Assert.Equal("stateChanger", exception.ParamName);
        }

        [Fact, GlobalLock(Reason = "Needs JobStorage.Current instance")]
        public void Ctor_UsesCurrent_JobStorageInstance_ByDefault()
        {
            JobStorage.Current = new Mock<JobStorage>().Object;
            // ReSharper disable once ObjectCreationAsStatement
            // Does not throw
            new BackgroundJobClient();
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
        public void CreateJob_DelegatesBackgroundJobCreation_ToFactory()
        {
            var client = CreateClient();

            client.Create(_job, _state.Object);

            _factory.Verify(x => x.Create(It.IsNotNull<CreateContext>()));
        }

        [Fact]
        public void CreateJob_ReturnsJobIdentifier()
        {
            var client = CreateClient();

            var id = client.Create(_job, _state.Object);

            Assert.Equal("some-job", id);
        }

        [Fact]
        public void CreateJob_WrapsOccurringExceptions_IntoItsOwnException()
        {
            var client = CreateClient();
            _factory.Setup(x => x.Create(It.IsAny<CreateContext>()))
                .Throws<InvalidOperationException>();

            var exception = Assert.Throws<BackgroundJobClientException>(
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

            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == "job-id" &&
                ctx.NewState == _state.Object &&
                ctx.ExpectedStates == null)));
        }

        [Fact]
        public void ChangeState_WithFromState_ChangesTheStateOfAJob_WithFromStateValue()
        {
            var client = CreateClient();

            client.ChangeState("job-id", _state.Object, "State");

            _stateChanger.Verify(x => x.ChangeState(It.Is<StateChangeContext>(ctx =>
                ctx.BackgroundJobId == "job-id" &&
                ctx.NewState == _state.Object &&
                ctx.ExpectedStates.SequenceEqual(new[] { "State" }))));
        }

        [Fact]
        public void ChangeState_ReturnsTheResult_OfStateChangerInvocation()
        {
            _stateChanger.Setup(x => x.ChangeState(It.IsAny<StateChangeContext>()))
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
            return new BackgroundJobClient(_storage.Object, _factory.Object, _stateChanger.Object);
        }
    }
}
