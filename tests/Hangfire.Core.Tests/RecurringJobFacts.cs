

using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hangfire.Common;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class RecurringJobFacts
    {
        private const string JobId = "job-id";
        private readonly Mock<IRecurringJobManager> _client;

        public RecurringJobFacts()
        {
            _client = new Mock<IRecurringJobManager>();
            RecurringJob.ClientFactory = () => _client.Object;
        }

        [Fact]
        public void AddOrUpdateShouldCreateJob_FromActionTypedExpression()
        {
            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            RecurringJob.AddOrUpdate(methodCall, TestClass.StaticGetCronExpression);

            var job = Job.FromExpression(methodCall);
            _client.Verify(x => x.AddOrUpdate(It.Is<string>(v => v == $"{job.Type.ToGenericTypeString()}.{job.Method.Name}"), 
                                                It.IsAny<Job>(),
                                                It.Is<string>(v => v == string.Empty),
                                                It.Is<RecurringJobOptions>(v => v.TimeZone.Equals(TimeZoneInfo.Utc))));
        }

        [Fact]
        public void GenericAddOrUpdateShouldCreateJob_FromActionTypedExpression()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            RecurringJob.AddOrUpdate<TestClass>(methodCall, TestClass.StaticGetCronExpression);

            var job = Job.FromExpression(methodCall);
            _client.Verify(x => x.AddOrUpdate(It.Is<string>(v => v == $"{job.Type.ToGenericTypeString()}.{job.Method.Name}"),
                                                It.IsAny<Job>(),
                                                It.Is<string>(v => v == string.Empty),
                                                It.Is<RecurringJobOptions>(v => v.TimeZone.Equals(TimeZoneInfo.Utc))));
        }

        [Fact]
        public void AddOrUpdateShouldCreateJob_WithJobId_FromActionTypedExpression()
        {
            Expression<Action> methodCall = () => TestClass.TestStaticMethod();

            RecurringJob.AddOrUpdate(JobId, methodCall, TestClass.StaticGetCronExpression);
            
            _client.Verify(x => x.AddOrUpdate(It.Is<string>(v => v == JobId),
                                                It.IsAny<Job>(),
                                                It.Is<string>(v => v == string.Empty),
                                                It.Is<RecurringJobOptions>(v => v.TimeZone.Equals(TimeZoneInfo.Utc))));
        }

        [Fact]
        public void GenericAddOrUpdateShouldCreateJob_WithJobId_FromActionTypedExpression()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            RecurringJob.AddOrUpdate<TestClass>(JobId, methodCall, TestClass.StaticGetCronExpression);
            
            _client.Verify(x => x.AddOrUpdate(It.Is<string>(v => v == JobId),
                                                It.IsAny<Job>(),
                                                It.Is<string>(v => v == string.Empty),
                                                It.Is<RecurringJobOptions>(v => v.TimeZone.Equals(TimeZoneInfo.Utc))));
        }

        [Fact]
        public void AddOrUpdateShouldCreateJob_FromFuncTaskTypedExpression()
        {
            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            RecurringJob.AddOrUpdate(methodCall, TestClass.StaticGetCronExpression);

            var job = Job.FromExpression(methodCall);
            _client.Verify(x => x.AddOrUpdate(It.Is<string>(v => v == $"{job.Type.ToGenericTypeString()}.{job.Method.Name}"),
                                                It.IsAny<Job>(),
                                                It.Is<string>(v => v == string.Empty),
                                                It.Is<RecurringJobOptions>(v => v.TimeZone.Equals(TimeZoneInfo.Utc))));
        }

        [Fact]
        public void GenericAddOrUpdateShouldCreateJob_FromFuncTaskTypedExpression()
        {
            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            RecurringJob.AddOrUpdate<TestClass>(methodCall, TestClass.StaticGetCronExpression);

            var job = Job.FromExpression(methodCall);
            _client.Verify(x => x.AddOrUpdate(It.Is<string>(v => v == $"{job.Type.ToGenericTypeString()}.{job.Method.Name}"),
                                                It.IsAny<Job>(),
                                                It.Is<string>(v => v == string.Empty),
                                                It.Is<RecurringJobOptions>(v => v.TimeZone.Equals(TimeZoneInfo.Utc))));
        }

        [Fact]
        public void AddOrUpdateShouldCreateJob_WithJobId_FromFuncTaskTypedExpression()
        {
            Expression<Func<Task>> methodCall = () => TestClass.TestStaticTaskMethod();

            RecurringJob.AddOrUpdate(JobId, methodCall, TestClass.StaticGetCronExpression);
            
            _client.Verify(x => x.AddOrUpdate(It.Is<string>(v => v == JobId),
                                                It.IsAny<Job>(),
                                                It.Is<string>(v => v == string.Empty),
                                                It.Is<RecurringJobOptions>(v => v.TimeZone.Equals(TimeZoneInfo.Utc))));
        }

        [Fact]
        public void GenericAddOrUpdateShouldCreateJob_WithJobId_FromFuncTaskTypedExpression()
        {
            Expression<Func<TestClass, Task>> methodCall = x => x.TestInstanceTaskMethod();

            RecurringJob.AddOrUpdate<TestClass>(JobId, methodCall, TestClass.StaticGetCronExpression);
            
            _client.Verify(x => x.AddOrUpdate(It.Is<string>(v => v == JobId),
                                                It.IsAny<Job>(),
                                                It.Is<string>(v => v == string.Empty),
                                                It.Is<RecurringJobOptions>(v => v.TimeZone.Equals(TimeZoneInfo.Utc))));
        }

        [Fact]
        public void AddOrUpdate_ShouldReferenceClassQueueAttribute_WhenMethodAttributeIsUnavailable()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestInstanceMethod();

            RecurringJob.AddOrUpdate<TestClass>(methodCall, TestClass.StaticGetCronExpression);

            _client.Verify(x => x.AddOrUpdate(
                It.IsAny<string>(),
                It.Is<Job>(v => v.QueueName == "foo"),
                It.Is<string>(v => v == string.Empty),
                It.Is<RecurringJobOptions>(v => v.TimeZone.Equals(TimeZoneInfo.Utc))));
        }

        [Fact]
        public void AddOrUpdate_ShouldReferenceMethodQueueAttribute_WhenMethodAttributeIsAvailable()
        {
            Expression<Action<TestClass>> methodCall = x => x.TestDecoratedInstanceMethod();

            RecurringJob.AddOrUpdate<TestClass>(methodCall, TestClass.StaticGetCronExpression);

            _client.Verify(x => x.AddOrUpdate(
                It.IsAny<string>(),
                It.Is<Job>(v => v.QueueName == "bar"),
                It.Is<string>(v => v == string.Empty),
                It.Is<RecurringJobOptions>(v => v.TimeZone.Equals(TimeZoneInfo.Utc))));
        }

        [Queue("foo")]
        public class TestClass
        {
            public static string StaticGetCronExpression()
            {
                return string.Empty;
            }

            public static void TestStaticMethod()
            {
                return;
            }

            public static Task TestStaticTaskMethod()
            {
                var source = new TaskCompletionSource<bool>();
                return source.Task;
            }

            public string InstanceGetCronExpression()
            {
                return string.Empty;
            }

            [Queue("bar")]
            public void TestDecoratedInstanceMethod()
            {
                return;
            }

            public void TestInstanceMethod()
            {
                return;
            }

            public Task TestInstanceTaskMethod()
            {
                var source = new TaskCompletionSource<bool>();
                return source.Task;
            }
        }
    }
}
