using System;
using HangFire.Client;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Client
{
    public class JobClientTests
    {
        private readonly JobClient _client;
        private readonly Mock<IStorageConnection> _connectionMock;
        private readonly Mock<JobCreator> _creatorMock;
        private readonly Mock<State> _stateMock;
        private readonly Job _job;

        public JobClientTests()
        {
            _connectionMock = new Mock<IStorageConnection>();
            _connectionMock.Setup(x => x.Storage).Returns(new Mock<JobStorage>().Object);

            _creatorMock = new Mock<JobCreator>();
            _client = new JobClient(_connectionMock.Object, _creatorMock.Object);
            _stateMock = new Mock<State>();
            _job = Job.FromExpression(() => Method());
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenClientManagerIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new JobClient(null, _creatorMock.Object));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobCreatorIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new JobClient(_connectionMock.Object, null));
        }

        [Fact]
        public void CreateJob_ThrowsAnException_WhenJobIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => _client.CreateJob(null, _stateMock.Object));
        }

        [Fact]
        public void CreateJob_ThrowsAnException_WhenStateIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => _client.CreateJob(_job, null));
        }

        public static void Method()
        {
        }

        public static void MethodWithReferenceParameter(ref string a)
        {
        }

        public static void MethodWithOutputParameter(out string a)
        {
            a = "hello";
        }
    }
}
