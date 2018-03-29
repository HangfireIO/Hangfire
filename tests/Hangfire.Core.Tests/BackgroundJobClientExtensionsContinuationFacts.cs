

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
    public class BackgroundJobClientExtensionsContinuationFacts
    {
        private const string ParentJobId = "parent-job-id";
        private const string JobId = "job-id";
        private const string CustomQueueName = "custom_queue";

        private readonly Mock<IBackgroundJobClient> _client;
        private readonly Mock<IState> _state;

        public BackgroundJobClientExtensionsContinuationFacts()
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
        public void ContinueWithShouldThrowException_ForStaticMethodCall_FromActionTypedExpression_WhenClientIsNull()
        {
            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.ContinueWith( 
                    null, ParentJobId, methodCall));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void ContinueWithShouldCreateJob_ForStaticMethodCall_FromActionTypedExpression()
        {
            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            _client.Object.ContinueWith(ParentJobId, methodCall);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticMethod" && v.Type.Name == "TestClass"),
                        It.Is<AwaitingState>(
                            v =>
                                v.NextState is EnqueuedState &&
                                ((EnqueuedState) v.NextState).Queue == EnqueuedState.DefaultQueue)));
        }

        [Fact]
        public void ContinueWithShouldCreateJob_WithCustomQueue_ForStaticMethodCall_FromActionTypedExpression()
        {
            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            _client.Object.ContinueWith(ParentJobId, methodCall, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticMethod" && v.Type.Name == "TestClass"),
                        It.Is<AwaitingState>(
                            v =>
                                v.NextState is EnqueuedState &&
                                ((EnqueuedState)v.NextState).Queue == CustomQueueName)));
        }

        [Fact]
        public void GenericContinueWithShouldThrowException_ForInstanceMethodCall_FromActionTypedExpression_WhenClientIsNull()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.ContinueWith(
                    null, ParentJobId, methodCall));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void GenericContinueWithShouldCreateJob_ForInstanceMethodCall_FromActionTypedExpression()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            _client.Object.ContinueWith(ParentJobId, methodCall);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceMethod" && v.Type.Name == "TestClass"),
                        It.Is<AwaitingState>(
                            v =>
                                v.NextState is EnqueuedState &&
                                ((EnqueuedState)v.NextState).Queue == EnqueuedState.DefaultQueue)));
        }

        [Fact]
        public void GenericContinueWithShouldCreateJob_WithCustomQueue_ForInstanceMethodCall_FromActionTypedExpression()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            _client.Object.ContinueWith(ParentJobId, methodCall, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceMethod" && v.Type.Name == "TestClass"),
                        It.Is<AwaitingState>(
                            v =>
                                v.NextState is EnqueuedState &&
                                ((EnqueuedState)v.NextState).Queue == CustomQueueName)));
        }

        [Fact]
        public void ContinueWithShouldThrowException_WithJobContinuationOptions_ForStaticMethodCall_FromActionTypedExpression_WhenClientIsNull()
        {
            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.ContinueWith(
                    null, ParentJobId, methodCall, JobContinuationOptions.OnAnyFinishedState));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void ContinueWithShouldCreateJob_WithJobContinuationOptions_ForStaticMethodCall_FromActionTypedExpression()
        {
            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            _client.Object.ContinueWith(ParentJobId, methodCall, JobContinuationOptions.OnAnyFinishedState);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticMethod" && v.Type.Name == "TestClass"),
                        It.Is<AwaitingState>(
                            v =>
                                v.NextState is EnqueuedState &&
                                ((EnqueuedState) v.NextState).Queue == EnqueuedState.DefaultQueue &&
                                v.Options == JobContinuationOptions.OnAnyFinishedState)));
        }

        [Fact]
        public void GenericContinueWithShouldThrowException_WithJobContinuationOptions_ForStaticMethodCall_FromActionTypedExpression_WhenClientIsNull()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.ContinueWith(
                    null, ParentJobId, methodCall, JobContinuationOptions.OnAnyFinishedState));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void GenericContinueWithShouldCreateJob_WithJobContinuationOptions_ForStaticMethodCall_FromActionTypedExpression()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            _client.Object.ContinueWith(ParentJobId, methodCall, JobContinuationOptions.OnAnyFinishedState);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceMethod" && v.Type.Name == "TestClass"),
                        It.Is<AwaitingState>(
                            v =>
                                v.NextState is EnqueuedState &&
                                ((EnqueuedState)v.NextState).Queue == EnqueuedState.DefaultQueue &&
                                v.Options == JobContinuationOptions.OnAnyFinishedState)));
        }

        [Fact]
        public void ContinueWithShouldThrowException_ForStaticMethodCall_FromFuncTaskTypedExpression_WhenClientIsNull()
        {
            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.ContinueWith(
                    null, ParentJobId, methodCall));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void ContinueWithShouldCreateJob_ForStaticMethodCall_FromFuncTaskTypedExpression()
        {
            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            _client.Object.ContinueWith(ParentJobId, methodCall);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<AwaitingState>(
                            v =>
                                v.NextState is EnqueuedState &&
                                ((EnqueuedState)v.NextState).Queue == EnqueuedState.DefaultQueue)));
        }

        [Fact]
        public void ContinueWithShouldCreateJob_WithJobContinuationOptions_ForStaticMethodCall_FromFuncTaskTypedExpression()
        {
            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            _client.Object.ContinueWith(ParentJobId, methodCall, null, JobContinuationOptions.OnAnyFinishedState);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<AwaitingState>(
                            v =>
                                v.NextState is EnqueuedState &&
                                ((EnqueuedState)v.NextState).Queue == EnqueuedState.DefaultQueue &&
                                v.Options == JobContinuationOptions.OnAnyFinishedState)));
        }

        [Fact]
        public void ContinueWithShouldCreateJob_WithCustomQueue_ForStaticMethodCall_FromFuncTaskTypedExpression()
        {
            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            _client.Object.ContinueWith(ParentJobId, methodCall, null, JobContinuationOptions.OnlyOnSucceededState, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestStaticTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<AwaitingState>(
                            v =>
                                v.NextState is EnqueuedState &&
                                ((EnqueuedState)v.NextState).Queue == CustomQueueName)));
        }

        [Fact]
        public void GenericContinueWithShouldThrowException_ForInstanceMethodCall_FromFuncTaskTypedExpression_WhenClientIsNull()
        {
            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.ContinueWith(
                    null, ParentJobId, methodCall));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void GenericContinueWithShouldCreateJob_ForInstanceMethodCall_FromFuncTaskTypedExpression()
        {
            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            _client.Object.ContinueWith(ParentJobId, methodCall);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<AwaitingState>(
                            v =>
                                v.NextState is EnqueuedState &&
                                ((EnqueuedState)v.NextState).Queue == EnqueuedState.DefaultQueue)));
        }

        [Fact]
        public void GenericContinueWithShouldCreateJob_WithJobContinuationOptions_ForInstanceMethodCall_FromFuncTaskTypedExpression()
        {
            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            _client.Object.ContinueWith<TestClass>(ParentJobId, methodCall, null, JobContinuationOptions.OnAnyFinishedState);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<AwaitingState>(
                            v =>
                                v.NextState is EnqueuedState &&
                                ((EnqueuedState) v.NextState).Queue == EnqueuedState.DefaultQueue &&
                                v.Options == JobContinuationOptions.OnAnyFinishedState)));
        }

        [Fact]
        public void GenericContinueWithShouldCreateJob_WithCustomQueue_ForInstanceMethodCall_FromFuncTaskTypedExpression()
        {
            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            _client.Object.ContinueWith<TestClass>(ParentJobId, methodCall, null,
                JobContinuationOptions.OnAnyFinishedState, CustomQueueName);

            _client.Verify(
                x =>
                    x.Create(It.Is<Job>(v => v.Method.Name == "TestInstanceTaskMethod" && v.Type.Name == "TestClass"),
                        It.Is<AwaitingState>(
                            v =>
                                v.NextState is EnqueuedState &&
                                ((EnqueuedState)v.NextState).Queue == CustomQueueName &&
                                v.Options == JobContinuationOptions.OnAnyFinishedState)));
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
