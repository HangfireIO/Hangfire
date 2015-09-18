using System;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class PerformContextFacts
    {
        private const string JobId = "id";

        private readonly WorkerContextMock _workerContext;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Job _job;
        private readonly DateTime _createdAt;
        private readonly Mock<IJobExecutionContext> _jobExecutionContext; 

        public PerformContextFacts()
        {
            _workerContext = new WorkerContextMock();
            _connection = new Mock<IStorageConnection>();
            _job = Job.FromExpression(() => Method());
            _createdAt = new DateTime(2012, 12, 12);
            _jobExecutionContext = new Mock<IJobExecutionContext>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenWorkerContextIsNull()
        {
            Assert.Throws<NullReferenceException>(
                () => new PerformContext(null, _connection.Object, JobId, _job, _createdAt, _jobExecutionContext.Object));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new PerformContext(_workerContext.Object, null, JobId, _job, _createdAt, _jobExecutionContext.Object));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new PerformContext(_workerContext.Object, _connection.Object, null, _job, _createdAt, _jobExecutionContext.Object));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new PerformContext(_workerContext.Object, _connection.Object, JobId, null, _createdAt, _jobExecutionContext.Object));

            Assert.Equal("job", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobExecutionContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new PerformContext(_workerContext.Object, _connection.Object, JobId, _job, _createdAt, null));

            Assert.Equal("jobExecutionContext", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var context = CreateContext();

            Assert.Equal(JobId, context.JobId);
            Assert.Equal(_createdAt, context.CreatedAt);
            Assert.NotNull(context.Items);
            Assert.Same(_connection.Object, context.Connection);
            Assert.Same(_job, context.Job);
            Assert.Same(_jobExecutionContext.Object, context.JobExecutionContext);
        }

        [Fact]
        public void CopyCtor_ThrowsAnException_WhenContextIsNull()
        {
            Assert.Throws<NullReferenceException>(
                () => new PerformContext(null));
        }

        [Fact]
        public void CopyCtor_CopiesAllPropertyValues()
        {
            var context = CreateContext();
            var contextCopy = new PerformContext(context);

            Assert.Equal(context.JobId, contextCopy.JobId);
            Assert.Equal(context.CreatedAt, contextCopy.CreatedAt);
            Assert.Same(context.Items, contextCopy.Items);
            Assert.Same(context.Connection, contextCopy.Connection);
            Assert.Same(context.Job, contextCopy.Job);
            Assert.Same(context.JobExecutionContext, contextCopy.JobExecutionContext);
        }

        [Fact]
        public void SetJobParameter_ThrowsAnException_WhenParameterNameIsNullOrEmpty()
        {
            var context = CreateContext();

            var exception = Assert.Throws<ArgumentNullException>(
                () => context.SetJobParameter(null, null));

            Assert.Equal("name", exception.ParamName);
        }

        [Fact]
        public void SetJobParameter_ConvertsValueToJson_AndSetsItUsingConnection()
        {
            var context = CreateContext();
            
            context.SetJobParameter("name", "value");

            _connection.Verify(x => x.SetJobParameter(JobId, "name", "\"value\""));
        }

        [Fact]
        public void GetJobParameter_ThrowsAnException_WhenNameIsNullOrEmpty()
        {
            var context = CreateContext();

            Assert.Throws<ArgumentNullException>(
                () => context.GetJobParameter<string>(null));
        }

        [Fact]
        public void GetJobParameter_ThrowsAnException_WhenParameterCouldNotBeDeserialized()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "name")).Returns("value");
            var context = CreateContext();

            Assert.Throws<InvalidOperationException>(
                () => context.GetJobParameter<int>("name"));
        }

        private PerformContext CreateContext()
        {
            return new PerformContext(_workerContext.Object, _connection.Object, JobId, _job, _createdAt, _jobExecutionContext.Object);
        }

        public static void Method() { }
    }
}
