

using System;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class BackgroundJobClientExtensionsChangeStateFacts
    {
        private const string JobId = "job-id";

        private readonly Mock<IBackgroundJobClient> _client;
        private readonly Mock<IState> _state;

        public BackgroundJobClientExtensionsChangeStateFacts()
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
        public void ChangeState_WithoutFromState_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.ChangeState(null, "job-id", _state.Object));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void ChangeState_WithoutFromState_CallsItsOverload()
        {
            _client.Object.ChangeState("job-id", _state.Object);

            _client.Verify(x => x.ChangeState("job-id", _state.Object, null));
        }

        [Fact]
        public void Requeue_ThrowsAnException_WhenClientIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => BackgroundJobClientExtensions.Requeue(null, JobId, FailedState.StateName));

            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void Requeue_ChangesTheStateOfAJob_ToEnqueued_InDefaultQueue()
        {
            _client.Object.Requeue(JobId);

            _client.Verify(
                x =>
                    x.ChangeState(JobId, It.Is<EnqueuedState>(state => state.Queue == EnqueuedState.DefaultQueue),
                        null));
        }

        [Fact]
        public void Requeue_WithFromState_ChangesTheStateOfAJob_ToEnqueued_InDefaultQueue_FromTheGivenState()
        {
            _client.Object.Requeue(JobId, FailedState.StateName);

            _client.Verify(
                x =>
                    x.ChangeState(JobId, It.Is<EnqueuedState>(state => state.Queue == EnqueuedState.DefaultQueue),
                        FailedState.StateName));
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
