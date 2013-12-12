using System;
using HangFire.Client;
using HangFire.States;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace HangFire.Tests
{
    [TestClass]
    public class ClientApiTests
    {
        private Mock<JobState> _stateMock;

        private Func<IJobClient> _oldClientFactory;
        private Mock<IJobClient> _jobClientMock;

        #region Tests initialization & clean up logic

        [TestInitialize]
        public void SetUp()
        {
            GlobalLock.Acquire();
            _oldClientFactory = BackgroundJob.ClientFactory;

            _jobClientMock = new Mock<IJobClient>();
            BackgroundJob.ClientFactory = () => _jobClientMock.Object;

            _stateMock = new Mock<JobState>("");
        }

        [TestCleanup]
        public void TearDown()
        {
            try
            {
                BackgroundJob.ClientFactory = _oldClientFactory;
            }
            finally
            {
                GlobalLock.Release();
            }
        }

        #endregion

        [TestMethod]
        public void Create_InstanceExpression_ReturnIdentifierReturnedByClient()
        {
            _jobClientMock
                .Setup(x => x.CreateJob(
                    It.IsAny<JobInvocationData>(), 
                    It.IsAny<string[]>(), 
                    It.IsAny<JobState>()))
                .Returns("id");

            var id = BackgroundJob.Create<TestService>(x => x.InstanceMethod(), _stateMock.Object);

            Assert.AreEqual("id", id);
        }

        [TestMethod]
        public void Create_StaticExpression_ReturnsIdentifierFromReturnedByClient()
        {
            _jobClientMock
                .Setup(x => x.CreateJob(
                    It.IsAny<JobInvocationData>(),
                    It.IsAny<string[]>(),
                    It.IsAny<JobState>()))
                .Returns("id");

            var id = BackgroundJob.Create(() => TestService.StaticMethod(), _stateMock.Object);

            Assert.AreEqual("id", id);
        }

        [TestMethod]
        public void Create_PassesState_ToClient()
        {
            BackgroundJob.Create<TestService>(x => x.InstanceMethod(), _stateMock.Object);

            _jobClientMock.Verify(client => client.CreateJob(
                It.IsAny<JobInvocationData>(),
                It.IsAny<string[]>(),
                It.Is<JobState>(x => x == _stateMock.Object)));
        }

        [TestMethod]
        public void Create_StaticExpression_CorrectlyDeterminesTypeAndMethod()
        {
            BackgroundJob.Create(() => TestService.StaticMethod(), _stateMock.Object);

            _jobClientMock.Verify(client => client.CreateJob(
                It.Is<JobInvocationData>(x => x.Type == typeof(TestService) && x.Method.Name == "StaticMethod"),
                It.Is<string[]>(x => x.Length == 0),
                It.IsAny<JobState>()));
        }

        [TestMethod]
        public void Create_InstanceExpression_CorrectlyDeterminesTypeAndMethod()
        {
            BackgroundJob.Create<TestService>(x => x.InstanceMethod(), _stateMock.Object);

            _jobClientMock.Verify(client => client.CreateJob(
                It.Is<JobInvocationData>(x => x.Type == typeof(TestService) && x.Method.Name == "InstanceMethod"),
                It.Is<string[]>(x => x.Length == 0),
                It.IsAny<JobState>()));
        }

        [TestMethod]
        public void Create_InstanceExpression_WithInterface_CorrectlyDeterminesTypeAndMethod()
        {
            BackgroundJob.Create<IService>(x => x.Method(), _stateMock.Object);

            _jobClientMock.Verify(client => client.CreateJob(
                It.Is<JobInvocationData>(x => x.Type == typeof(IService) && x.Method.Name == "Method"),
                It.Is<string[]>(x => x.Length == 0),
                It.IsAny<JobState>()));
        }

        [TestMethod]
        public void Create_InstanceExpression_PassesSpecifiedTypeToClient_NotTheDeclaringOne()
        {
            BackgroundJob.Create<DerivedTestService>(x => x.InstanceMethod(), _stateMock.Object);

            _jobClientMock.Verify(client => client.CreateJob(
                It.Is<JobInvocationData>(x => x.Type == typeof(DerivedTestService)),
                It.IsAny<string[]>(),
                It.IsAny<JobState>()));
        }

        [TestMethod]
        public void Create_StaticExpression_PassesDeclaringType_ToClient()
        {
            BackgroundJob.Enqueue(() => DerivedTestService.StaticMethod());

            _jobClientMock.Verify(client => client.CreateJob(
                It.Is<JobInvocationData>(x => x.Type == typeof(DerivedTestService)),
                It.IsAny<string[]>(),
                It.IsAny<JobState>()));
        }

        [TestMethod]
        public void Create_InstanceExpression_PassesParameterValuesAndTheirTypes_ToClient()
        {
            BackgroundJob.Create<TestService>(x => x.InstanceMethod("Hello"), _stateMock.Object);

            _jobClientMock.Verify(client => client.CreateJob(
                It.IsAny<JobInvocationData>(),
                It.Is<string[]>(x => x[0] == "Hello"),
                It.IsAny<JobState>()));
        }

        [TestMethod]
        public void Create_StaticExpression_PassesParameterValuesAndTheirTypes_ToClient()
        {
            BackgroundJob.Create(() => TestService.StaticMethod(34), _stateMock.Object);

            _jobClientMock.Verify(client => client.CreateJob(
                It.IsAny<JobInvocationData>(),
                It.Is<string[]>(x => x[0] == "34"),
                It.IsAny<JobState>()));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Create_PassingParametersByReference_IsNotAllowed()
        {
            int a = 10;
            BackgroundJob.Create<DerivedTestService>(x => x.InstanceMethod(ref a), _stateMock.Object);

            Assert.Inconclusive("Move to JobClient tests");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Create_PassingOutputParameters_IsNotAllowed()
        {
            int a;
            BackgroundJob.Enqueue<DerivedTestService>(x => x.OutMethod(out a));

            Assert.Inconclusive("Move to JobClient tests");
        }

        [TestMethod]
        public void Enqueue_InstanceExpression_PassesEnqueuedState_ToClient()
        {
            BackgroundJob.Enqueue<TestService>(x => x.InstanceMethod());

            _jobClientMock.Verify(client => client.CreateJob(
                It.IsAny<JobInvocationData>(),
                It.IsAny<string[]>(),
                It.Is<JobState>(x => x is EnqueuedState)));
        }

        [TestMethod]
        public void Enqueue_StaticExpression_PassesEnqueuedState_ToClient()
        {
            BackgroundJob.Enqueue(() => TestService.StaticMethod());

            _jobClientMock.Verify(client => client.CreateJob(
                It.IsAny<JobInvocationData>(),
                It.IsAny<string[]>(),
                It.Is<JobState>(x => x is EnqueuedState)));
        }

        [TestMethod]
        public void Schedule_InstanceExpression_PassesCorrectScheduledState_ToClient()
        {
            BackgroundJob.Schedule<TestService>(x => x.InstanceMethod(), TimeSpan.FromHours(5));

            _jobClientMock.Verify(x => x.CreateJob(
                It.IsAny<JobInvocationData>(),
                It.IsAny<string[]>(),
                It.Is<JobState>(y => 
                    y is ScheduledState 
                    && 
                    ((ScheduledState)y).EnqueueAt > DateTime.UtcNow.AddHours(4)
                    && ((ScheduledState)y).EnqueueAt < DateTime.UtcNow.AddHours(6))));
        }

        [TestMethod]
        public void Schedule_StaticExpression_PassesCorrectScheduledState_ToClient()
        {
            BackgroundJob.Schedule(() => TestService.StaticMethod(), TimeSpan.FromHours(5));

            _jobClientMock.Verify(x => x.CreateJob(
                It.IsAny<JobInvocationData>(),
                It.IsAny<string[]>(),
                It.Is<JobState>(y =>
                    y is ScheduledState
                    &&
                    ((ScheduledState)y).EnqueueAt > DateTime.UtcNow.AddHours(4)
                    && ((ScheduledState)y).EnqueueAt < DateTime.UtcNow.AddHours(6))));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ClientFactory_Set_ExceptionIsThrownWhenTheValueIsNull()
        {
            BackgroundJob.ClientFactory = null;
        }

        public class TestService
        {
            public void InstanceMethod()
            {
            }

            public void InstanceMethod(string a)
            {
            }

            public static void StaticMethod()
            {
            }

            public static void StaticMethod(int a)
            {
            }

            public void InstanceMethod(ref int a)
            {
            }

            public void OutMethod(out int a)
            {
                a = 10;
            }
        }

        public interface IService
        {
            void Method();
        }

        public class DerivedTestService : TestService
        {
            public static void StaticMethod()
            {
            }
        }
    }
}
