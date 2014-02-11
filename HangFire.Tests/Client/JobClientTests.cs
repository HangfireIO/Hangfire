using System;
using HangFire.Client;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ServiceStack.Redis;

namespace HangFire.Tests.Client
{
    [TestClass]
    public class JobClientTests
    {
        private JobClient _client;
        private Mock<IStorageConnection> _connectionMock;
        private Mock<JobCreator> _creatorMock;
        private Mock<JobState> _stateMock;
        private JobMethod _method;

        [TestInitialize]
        public void Initialize()
        {
            _connectionMock = new Mock<IStorageConnection>();

            _creatorMock = new Mock<JobCreator>();
            _client = new JobClient(_connectionMock.Object, _creatorMock.Object);
            _stateMock = new Mock<JobState>("SomeReason");
            _method = new JobMethod(typeof(JobClientTests), typeof(JobClientTests).GetMethod("Method"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_ThrowsAnException_WhenClientManagerIsNull()
        {
// ReSharper disable ObjectCreationAsStatement
            new JobClient(null, _creatorMock.Object);
// ReSharper restore ObjectCreationAsStatement
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void Ctor_ThrowsAnException_WhenJobCreatorIsNull()
        {
// ReSharper disable ObjectCreationAsStatement
            new JobClient(_connectionMock.Object, null);
// ReSharper restore ObjectCreationAsStatement
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateJob_ThrowsAnException_WhenJobMethodIsNull()
        {
            _client.CreateJob(null, new string[0], _stateMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateJob_ThrowsAnException_WhenArgumentsIsNull()
        {
            _client.CreateJob(_method, null, _stateMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateJob_ThrowsAnException_WhenStateIsNull()
        {
            _client.CreateJob(_method, new string[0], null);
        }

        [TestMethod]
        public void CreateJob_Returns_AnUniqueIdentifier()
        {
            var id = _client.CreateJob(_method, new string[0], _stateMock.Object);
            var guid = Guid.Parse(id);

            Assert.AreNotEqual(Guid.Empty, guid);
        }

        [TestMethod]
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
