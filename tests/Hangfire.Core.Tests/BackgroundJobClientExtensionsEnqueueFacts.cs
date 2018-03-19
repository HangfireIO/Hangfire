using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class BackgroundJobClientExtensionsEnqueueFacts
    {
        private const string JobId = "job-id";
        private const string CustomQueueName = "custom_queue";

        private readonly Mock<IBackgroundJobClient> _client;
        private readonly Mock<IState> _state;

        public BackgroundJobClientExtensionsEnqueueFacts()
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
        public void EnqueueShouldThrowException_ForStaticMethodCall_FromActionTypedExpression_WhenClientIsNull()
        {
            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Enqueue(
                    null, methodCall));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void EnqueueShouldCreateJob_ForStaticMethodCall_FromActionTypedExpression()
        {
            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            _client.Object.Enqueue(methodCall);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticMethod" && v.Type.Name == "TestClass"),
                        It.Is<EnqueuedState>(v => v.Queue == EnqueuedState.DefaultQueue)));
        }

        [Fact]
        public void EnqueueShouldCreateJob_WithCustomQueue_ForStaticMethodCall_FromActionTypedExpression()
        {
            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            _client.Object.Enqueue(methodCall, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticMethod" && v.Type.Name == "TestClass"),
                        It.Is<EnqueuedState>(v => v.Queue == CustomQueueName)));
        }

        [Fact]
        public void EnqueueShouldThrowException_ForStaticMethodCall_FromFuncTaskExpression_WhenClientIsNull()
        {
            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Enqueue(
                    null, methodCall));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void EnqueueShouldCreateJob_ForStaticMethodCall_FromFuncTaskExpression()
        {
            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            _client.Object.Enqueue(methodCall);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<EnqueuedState>(v => v.Queue == EnqueuedState.DefaultQueue)));
        }

        [Fact]
        public void EnqueueShouldCreateJob_WithCustomQueue_ForStaticMethodCall_FromFuncTaskExpression()
        {
            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            _client.Object.Enqueue(methodCall, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<EnqueuedState>(v => v.Queue == CustomQueueName)));
        }

        [Fact]
        public void GenericEnqueueShouldThrowException_ForInstanceMethodCall_FromActionTypedExpression_WhenClientIsNull()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Enqueue<TestClass>(
                    null, methodCall));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void GenericEnqueueShouldCreateJob_ForInstanceMethodCall_FromActionTypedExpression()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            _client.Object.Enqueue<TestClass>(methodCall);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceMethod" && v.Type.Name == "TestClass"),
                        It.Is<EnqueuedState>(v => v.Queue == EnqueuedState.DefaultQueue)));
        }

        [Fact]
        public void GenericEnqueueShouldCreateJob_WithCustomQueue_ForInstanceMethodCall_FromActionTypedExpression()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            _client.Object.Enqueue<TestClass>(methodCall, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceMethod" && v.Type.Name == "TestClass"),
                        It.Is<EnqueuedState>(v => v.Queue == CustomQueueName)));
        }

        [Fact]
        public void GenericEnqueueShouldThrowException_ForInstanceMethodCall_FromFuncTaskExpression_WhenClientIsNull()
        {
            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Enqueue<TestClass>(
                    null, methodCall));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void GenericEnqueueShouldCreateJob_ForInstanceMethodCall_FromFuncTaskExpression()
        {
            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            _client.Object.Enqueue<TestClass>(methodCall);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<EnqueuedState>(v => v.Queue == EnqueuedState.DefaultQueue)));
        }

        [Fact]
        public void GenericEnqueueShouldCreateJob_WithCustomQueue_ForInstanceMethodCall_FromFuncTaskExpression()
        {
            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            _client.Object.Enqueue<TestClass>(methodCall, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<EnqueuedState>(v => v.Queue == CustomQueueName)));
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
