using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests
{
    public class BackgroundJobClientExtensionsCreateFacts
    {
        private const string JobId = "job-id";

        private readonly Mock<IBackgroundJobClient> _client;
        private readonly Mock<IState> _state;

        public BackgroundJobClientExtensionsCreateFacts()
        {
            _client = new Mock<IBackgroundJobClient>();
            _state = new Mock<IState>();

            var job = Job.FromExpression<TestClass>(x => x.TestInstanceMethod());

            var jobData = new JobData { Job = job };

            var connection = new Mock<IStorageConnection>();
            connection.Setup(x => x.GetJobData(JobId)).Returns(jobData);

            var storage = new Mock<JobStorage>();
            storage.Setup(x => x.GetConnection()).Returns(connection.Object);

            JobStorage.Current = storage.Object;
        }

        [Fact]
        public void CreateShouldThrowException_ForStaticMethodCall_FromActionTypedExpression_WhenClientIsNull()
        {
            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Create(
                    null, methodCall, _state.Object));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void CreateShouldCreateAJobInTheGivenState_ForStaticMethodCall_FromActionTypedExpression()
        {
            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            _client.Object.Create(methodCall, _state.Object);
            
            _client.Verify(x => x.Create(It.IsNotNull<Job>(), _state.Object));
        }
        
        [Fact]
        public void CreateShouldThrowException_ForStaticMethodCall_FromFuncTaskTypedExpression_WhenClientIsNull()
        {
            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Create(
                    null, methodCall, _state.Object));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void CreateShouldCreateAJobInTheGivenState_ForStaticMethodCall_FromFuncTaskTypedExpression()
        {
            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            _client.Object.Create(methodCall, _state.Object);

            _client.Verify(x => x.Create(It.IsNotNull<Job>(), _state.Object));
        }

        [Fact]
        public void GenericCreateShouldThrowException_ForInstanceMethodCall_FromActionTypedExpression_WhenClientIsNull()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Create(
                    null, methodCall, _state.Object));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void GenericCreateShouldCreateAJobInTheGivenState_ForInstanceMethodCall_FromActionTypedExpression()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            _client.Object.Create(methodCall, _state.Object);

            _client.Verify(x => x.Create(It.IsNotNull<Job>(), _state.Object));
        }

        [Fact]
        public void GenericCreateShouldThrowException_ForInstanceMethodCall_FromFuncTaskTypedExpression_WhenClientIsNull()
        {
            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Create(
                    null, methodCall, _state.Object));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void GenericCreateShouldCreateAJobInTheGivenState_ForInstanceMethodCall_FromFuncTaskTypedExpression()
        {
            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            _client.Object.Create(methodCall, _state.Object);

            _client.Verify(x => x.Create(It.IsNotNull<Job>(), _state.Object));
        }

        public class TestClass
        {
            public static void TestStaticMethod()
            {


            }

            public static Task TestStaticTaskMethod()
            {
                var source = new TaskCompletionSource<bool>();
                return source.Task;
            }

            public void TestInstanceMethod()
            {

            }

            public Task TestInstanceTaskMethod()
            {
                var source = new TaskCompletionSource<bool>();
                return source.Task;
            }
        }
    }
}
