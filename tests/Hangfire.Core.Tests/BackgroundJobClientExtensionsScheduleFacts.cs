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
    public class BackgroundJobClientExtensionsScheduleFacts
    {
        private const string JobId = "job-id";
        private const string CustomQueueName = "custom_queue";

        private readonly Mock<IBackgroundJobClient> _client;
        private readonly Mock<IState> _state;

        public BackgroundJobClientExtensionsScheduleFacts()
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
        public void ScheduleShouldThrowException_WithDelay_ForStaticMethodCall_FromActionTypedExpression_WhenClientIsNull()
        {
            var runAt = DateTime.UtcNow.AddHours(1);
            var delay = runAt.Subtract(DateTime.UtcNow);

            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Schedule(
                    null, methodCall, delay));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void ScheduleShouldCreateJob_WithDelay_ForStaticMethodCall_FromActionTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);
            var delay = runAt.Subtract(DateTime.UtcNow);

            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            _client.Object.Schedule(methodCall, delay);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(
                            v => v.QueueName == EnqueuedState.DefaultQueue && v.EnqueueAt.Hour == runAt.Hour)));
        }

        [Fact]
        public void ScheduleShouldCreateJob_WithDelayAndCustomQueue_ForStaticMethodCall_FromActionTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);
            var delay = runAt.Subtract(DateTime.UtcNow);

            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            _client.Object.Schedule(methodCall, delay, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(
                            v => v.QueueName == CustomQueueName && v.EnqueueAt.Hour == runAt.Hour)));
        }

        [Fact]
        public void ScheduleShouldThrowException_WithDelay_ForStaticMethodCall_FromFuncTaskTypedExpression_WhenClientIsNull()
        {
            var runAt = DateTime.UtcNow.AddHours(1);
            var delay = runAt.Subtract(DateTime.UtcNow);

            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Schedule(
                    null, methodCall, delay));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void ScheduleShouldCreateJob_WithDelay_ForStaticMethodCall_FromFuncTaskTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);
            var delay = runAt.Subtract(DateTime.UtcNow);

            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            _client.Object.Schedule(methodCall, delay);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(
                            v => v.QueueName == EnqueuedState.DefaultQueue && v.EnqueueAt.Hour == runAt.Hour)));
        }

        [Fact]
        public void ScheduleShouldCreateJob_WithDelayAndCustomQueueName_ForStaticMethodCall_FromFuncTaskTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);
            var delay = runAt.Subtract(DateTime.UtcNow);

            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            _client.Object.Schedule(methodCall, delay, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(
                            v => v.QueueName == CustomQueueName && v.EnqueueAt.Hour == runAt.Hour)));
        }

        [Fact]
        public void GenericScheduleShouldThrowException_WithDelay_ForInstanceMethodCall_FromActionTypedExpression_WhenClientIsNull()
        {
            var runAt = DateTime.UtcNow.AddHours(1);
            var delay = runAt.Subtract(DateTime.UtcNow);

            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Schedule<TestClass>(
                    null, methodCall, delay));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void GenericScheduleShouldCreateJob_WithDelay_ForInstanceMethodCall_FromActionTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);
            var delay = runAt.Subtract(DateTime.UtcNow);

            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            _client.Object.Schedule<TestClass>(methodCall, delay);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(
                            v => v.QueueName == EnqueuedState.DefaultQueue && v.EnqueueAt.Hour == runAt.Hour)));
        }

        [Fact]
        public void GenericScheduleShouldCreateJob_WithDelayAndCustomQueue_ForInstanceMethodCall_FromActionTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);
            var delay = runAt.Subtract(DateTime.UtcNow);

            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            _client.Object.Schedule<TestClass>(methodCall, delay, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(v => v.QueueName == CustomQueueName && v.EnqueueAt.Hour == runAt.Hour)));
        }

        [Fact]
        public void GenericScheduleShouldThrowException_WithDelay_ForInstanceMethodCall_FromFuncTaskTypedExpression_WhenClientIsNull()
        {
            var runAt = DateTime.UtcNow.AddHours(1);
            var delay = runAt.Subtract(DateTime.UtcNow);

            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Schedule<TestClass>(
                    null, methodCall, delay));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void GenericScheduleShouldCreateJob_WithDelay_ForInstanceMethodCall_FromFuncTaskTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);
            var delay = runAt.Subtract(DateTime.UtcNow);

            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            _client.Object.Schedule<TestClass>(methodCall, delay);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(
                            v => v.QueueName == EnqueuedState.DefaultQueue && v.EnqueueAt.Hour == runAt.Hour)));
        }

        [Fact]
        public void GenericScheduleShouldCreateJob_WithDelayAndCustomQueue_ForInstanceMethodCall_FromFuncTaskTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);
            var delay = runAt.Subtract(DateTime.UtcNow);

            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            _client.Object.Schedule<TestClass>(methodCall, delay, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(
                            v => v.QueueName == CustomQueueName && v.EnqueueAt.Hour == runAt.Hour)));
        }

        [Fact]
        public void ScheduleShouldThrowException_WithSetRunTime_ForStaticMethodCall_FromActionTypedExpression_WhenClientIsNull()
        {
            var runAt = DateTime.UtcNow.AddHours(1);

            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Schedule(
                    null, methodCall, runAt));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void ScheduleShouldCreateJob_WithSetRunTime_ForStaticMethodCall_FromActionTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);

            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            _client.Object.Schedule(methodCall, runAt);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(v => v.QueueName == EnqueuedState.DefaultQueue && v.EnqueueAt == runAt)));
        }

        [Fact]
        public void ScheduleShouldCreateJob_WithSetRunTimeAndCustomQueue_ForStaticMethodCall_FromActionTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);

            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            _client.Object.Schedule(methodCall, runAt, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(v => v.QueueName == CustomQueueName && v.EnqueueAt == runAt)));
        }

        [Fact]
        public void ScheduleShouldThrowException_WithSetRunTime_ForStaticMethodCall_FromFuncTaskTypedExpression_WhenClientIsNull()
        {
            var runAt = DateTime.UtcNow.AddHours(1);

            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Schedule(
                    null, methodCall, runAt));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void ScheduleShouldCreateJob_WithSetRunTime_ForStaticMethodCall_FromFuncTaskTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);

            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            _client.Object.Schedule(methodCall, runAt);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(v => v.QueueName == EnqueuedState.DefaultQueue && v.EnqueueAt == runAt)));
        }

        [Fact]
        public void ScheduleShouldCreateJob_WithSetRunTimeAndCustomQueue_ForStaticMethodCall_FromFuncTaskTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);

            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            _client.Object.Schedule(methodCall, runAt, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(v => v.QueueName == CustomQueueName && v.EnqueueAt == runAt)));
        }

        [Fact]
        public void GenericScheduleShouldThrowException_WithSetRunTime_ForInstanceMethodCall_FromActionTypedExpression_WhenClientIsNull()
        {
            var runAt = DateTime.UtcNow.AddHours(1);

            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Schedule<TestClass>(
                    null, methodCall, runAt));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void GenericScheduleShouldCreateJob_WithSetRunTime_ForInstanceMethodCall_FromActionTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);

            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            _client.Object.Schedule<TestClass>(methodCall, runAt);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(v => v.QueueName == EnqueuedState.DefaultQueue && v.EnqueueAt == runAt)));
        }

        [Fact]
        public void GenericScheduleShouldCreateJob_WithSetRunTimeAndCustomQueue_ForInstanceMethodCall_FromActionTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);

            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            _client.Object.Schedule<TestClass>(methodCall, runAt, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(v => v.QueueName == CustomQueueName && v.EnqueueAt == runAt)));
        }

        [Fact]
        public void GenericScheduleShouldThrowException_WithSetRunTime_ForInstanceMethodCall_FromFuncTaskTypedExpression_WhenClientIsNull()
        {
            var runAt = DateTime.UtcNow.AddHours(1);

            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Schedule<TestClass>(
                    null, methodCall, runAt));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void GenericScheduleShouldCreateJob_WithSetRunTime_ForInstanceMethodCall_FromFuncTaskTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);

            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            _client.Object.Schedule<TestClass>(methodCall, runAt);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(v => v.QueueName == EnqueuedState.DefaultQueue && v.EnqueueAt == runAt)));
        }

        [Fact]
        public void GenericScheduleShouldCreateJob_WithSetRunTimeAndCustomQueue_ForInstanceMethodCall_FromFuncTaskTypedExpression()
        {
            var runAt = DateTime.UtcNow.AddHours(1);

            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            _client.Object.Schedule<TestClass>(methodCall, runAt, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<ScheduledState>(v => v.QueueName == CustomQueueName && v.EnqueueAt == runAt)));
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
