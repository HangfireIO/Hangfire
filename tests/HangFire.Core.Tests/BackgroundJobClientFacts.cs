using System;
using HangFire.Client;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests
{
    public class BackgroundJobClientFacts
    {
        private readonly BackgroundJobClient _client;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IJobCreationProcess> _pipeline;
        private readonly Mock<State> _state;
        private readonly Job _job;

        public BackgroundJobClientFacts()
        {
            _connection = new Mock<IStorageConnection>();
            _connection.Setup(x => x.Storage).Returns(new Mock<JobStorage>().Object);

            _pipeline = new Mock<IJobCreationProcess>();
            _client = new BackgroundJobClient(_connection.Object, _pipeline.Object);
            _state = new Mock<State>();
            _job = Job.FromExpression(() => Method());
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenClientManagerIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobClient(null, _pipeline.Object));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobCreatorIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BackgroundJobClient(_connection.Object, null));
        }

        [Fact]
        public void CreateJob_ThrowsAnException_WhenJobIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => _client.Create(null, _state.Object));
        }

        [Fact]
        public void CreateJob_ThrowsAnException_WhenStateIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => _client.Create(_job, null));
        }

        public static void Method()
        {
        }
    }
}
