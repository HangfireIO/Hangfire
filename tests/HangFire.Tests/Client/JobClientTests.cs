using System;
using HangFire.Client;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Storage;
using Moq;
using ServiceStack.Redis;
using Xunit;

namespace HangFire.Tests.Client
{
    public class JobClientTests
    {
        private readonly JobClient _client;
        private readonly Mock<IStorageConnection> _connectionMock;
        private readonly Mock<JobCreator> _creatorMock;
        private readonly Mock<JobState> _stateMock;
        private readonly JobMethod _method;

        public JobClientTests()
        {
            _connectionMock = new Mock<IStorageConnection>();
            _connectionMock.Setup(x => x.Storage).Returns(new Mock<JobStorage>().Object);

            _creatorMock = new Mock<JobCreator>();
            _client = new JobClient(_connectionMock.Object, _creatorMock.Object);
            _stateMock = new Mock<JobState>();
            _method = new JobMethod(typeof(JobClientTests), typeof(JobClientTests).GetMethod("Method"));
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
        public void CreateJob_ThrowsAnException_WhenJobMethodIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => _client.CreateJob(null, new string[0], _stateMock.Object));
        }

        [Fact]
        public void CreateJob_ThrowsAnException_WhenArgumentsIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => _client.CreateJob(_method, null, _stateMock.Object));
        }

        [Fact]
        public void CreateJob_ThrowsAnException_WhenStateIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => _client.CreateJob(_method, new string[0], null));
        }

        [Fact]
        public void CreateJob_CallsCreate_WithCorrectContext()
        {
            _client.CreateJob(_method, new[] { "hello", "3" }, _stateMock.Object);
        }

        public void Method()
        {
        }

        public void MethodWithReferenceParameter(ref string a)
        {
        }

        public void MethodWithOutputParameter(out string a)
        {
            a = "hello";
        }
    }
}
