using System;
using HangFire.Client;
using HangFire.Common;
using HangFire.Common.States;
using HangFire.States;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace HangFire.Tests
{
    public class ClientApiTests : IDisposable
    {
        private readonly Mock<State> _stateMock;

        private readonly Func<IBackgroundJobClient> _oldClientFactory;
        private readonly Mock<IBackgroundJobClient> _jobClientMock;

        #region Tests initialization & clean up logic

        public ClientApiTests()
        {
            GlobalLock.Acquire();
            _oldClientFactory = BackgroundJob.ClientFactory;

            _jobClientMock = new Mock<IBackgroundJobClient>();
            BackgroundJob.ClientFactory = () => _jobClientMock.Object;

            _stateMock = new Mock<State>();
        }

        public void Dispose()
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

        [Fact]
        public void Create_InstanceExpression_ReturnIdentifierReturnedByClient()
        {
            _jobClientMock
                .Setup(x => x.Create(
                    It.IsAny<Job>(),
                    It.IsAny<State>()))
                .Returns("id");

            /*var id = BackgroundJob.Create<TestService>(x => x.InstanceMethod(), _stateMock.Object);

            Assert.Equal("id", id);*/
        }

        [Fact]
        public void Create_StaticExpression_ReturnsIdentifierFromReturnedByClient()
        {
            _jobClientMock
                .Setup(x => x.Create(
                    It.IsAny<Job>(),
                    It.IsAny<State>()))
                .Returns("id");

            /*var id = BackgroundJob.Create(() => TestService.StaticMethod(), _stateMock.Object);

            Assert.Equal("id", id);*/
        }

        [Fact]
        public void Create_PassesState_ToClient()
        {
            //BackgroundJob.Create<TestService>(x => x.InstanceMethod(), _stateMock.Object);

            _jobClientMock.Verify(client => client.Create(
                It.IsAny<Job>(),
                It.Is<State>(x => x == _stateMock.Object)));
        }

        [Fact]
        public void Create_StaticExpression_CorrectlyDeterminesTypeAndMethod()
        {
            //BackgroundJob.Create(() => TestService.StaticMethod(), _stateMock.Object);

            /*_jobClientMock.Verify(client => client.CreateJob(
                It.Is<JobMethod>(x => x.Type == typeof(TestService) && x.MethodInfo.Name == "StaticMethod"),
                It.Is<string[]>(x => x.Length == 0),
                It.IsAny<State>()));*/
        }

        [Fact]
        public void Create_InstanceExpression_CorrectlyDeterminesTypeAndMethod()
        {
            //BackgroundJob.Create<TestService>(x => x.InstanceMethod(), _stateMock.Object);

            /*_jobClientMock.Verify(client => client.CreateJob(
                It.Is<JobMethod>(x => x.Type == typeof(TestService) && x.MethodInfo.Name == "InstanceMethod"),
                It.Is<string[]>(x => x.Length == 0),
                It.IsAny<State>()));*/
        }

        [Fact]
        public void Create_InstanceExpression_WithInterface_CorrectlyDeterminesTypeAndMethod()
        {
            /*BackgroundJob.Create<IService>(x => x.Method(), _stateMock.Object);

            _jobClientMock.Verify(client => client.CreateJob(
                It.Is<JobMethod>(x => x.Type == typeof(IService) && x.MethodInfo.Name == "Method"),
                It.Is<string[]>(x => x.Length == 0),
                It.IsAny<State>()));*/
        }

        [Fact]
        public void Create_InstanceExpression_PassesSpecifiedTypeToClient_NotTheDeclaringOne()
        {
            /*BackgroundJob.Create<DerivedTestService>(x => x.InstanceMethod(), _stateMock.Object);

            _jobClientMock.Verify(client => client.CreateJob(
                It.Is<JobMethod>(x => x.Type == typeof(DerivedTestService)),
                It.IsAny<string[]>(),
                It.IsAny<State>()));*/
        }

        [Fact]
        public void Create_StaticExpression_PassesDeclaringType_ToClient()
        {
            BackgroundJob.Enqueue(() => DerivedTestService.StaticMethod());

            /*_jobClientMock.Verify(client => client.CreateJob(
                It.Is<JobMethod>(x => x.Type == typeof(DerivedTestService)),
                It.IsAny<string[]>(),
                It.IsAny<State>()));*/
        }

        [Fact]
        public void Create_InstanceExpression_PassesParameterValuesAndTheirTypes_ToClient()
        {
            /*BackgroundJob.Create<TestService>(x => x.InstanceMethod("Hello"), _stateMock.Object);

            _jobClientMock.Verify(client => client.CreateJob(
                It.IsAny<JobMethod>(),
                It.Is<string[]>(x => x[0] == "Hello"),
                It.IsAny<State>()));*/
        }

        [Fact]
        public void Create_StaticExpression_PassesParameterValuesAndTheirTypes_ToClient()
        {
            /*BackgroundJob.Create(() => TestService.StaticMethod(34), _stateMock.Object);

            _jobClientMock.Verify(client => client.CreateJob(
                It.IsAny<JobMethod>(),
                It.Is<string[]>(x => x[0] == "34"),
                It.IsAny<State>()));*/
        }

        [Fact]
        public void Create_PassingParametersByReference_IsNotAllowed()
        {
            int a = 10;

            /*Assert.Throws<ArgumentException>(
                () => BackgroundJob.Create<DerivedTestService>(x => x.InstanceMethod(ref a), _stateMock.Object));*/
        }

        [Fact]
        public void Create_PassingOutputParameters_IsNotAllowed()
        {
            int a;

            Assert.Throws<ArgumentException>(
                () => BackgroundJob.Enqueue<DerivedTestService>(x => x.OutMethod(out a)));
        }

        [Fact]
        public void Enqueue_InstanceExpression_PassesEnqueuedState_ToClient()
        {
            BackgroundJob.Enqueue<TestService>(x => x.InstanceMethod());

            _jobClientMock.Verify(client => client.Create(
                It.IsAny<Job>(),
                It.Is<State>(x => x is EnqueuedState)));
        }

        [Fact]
        public void Enqueue_StaticExpression_PassesEnqueuedState_ToClient()
        {
            BackgroundJob.Enqueue(() => TestService.StaticMethod());

            _jobClientMock.Verify(client => client.Create(
                It.IsAny<Job>(),
                It.Is<State>(x => x is EnqueuedState)));
        }

        [Fact]
        public void Schedule_InstanceExpression_PassesCorrectScheduledState_ToClient()
        {
            BackgroundJob.Schedule<TestService>(x => x.InstanceMethod(), TimeSpan.FromHours(5));

            _jobClientMock.Verify(x => x.Create(
                It.IsAny<Job>(),
                It.Is<State>(y => 
                    y is ScheduledState 
                    && 
                    ((ScheduledState)y).EnqueueAt > DateTime.UtcNow.AddHours(4)
                    && ((ScheduledState)y).EnqueueAt < DateTime.UtcNow.AddHours(6))));
        }

        [Fact]
        public void Schedule_StaticExpression_PassesCorrectScheduledState_ToClient()
        {
            BackgroundJob.Schedule(() => TestService.StaticMethod(), TimeSpan.FromHours(5));

            _jobClientMock.Verify(x => x.Create(
                It.IsAny<Job>(),
                It.Is<State>(y =>
                    y is ScheduledState
                    &&
                    ((ScheduledState)y).EnqueueAt > DateTime.UtcNow.AddHours(4)
                    && ((ScheduledState)y).EnqueueAt < DateTime.UtcNow.AddHours(6))));
        }

        [Fact]
        public void ClientFactory_Set_ExceptionIsThrownWhenTheValueIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => BackgroundJob.ClientFactory = null);
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
