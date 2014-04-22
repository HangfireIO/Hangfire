using System;
using HangFire.Common;
using HangFire.Server;
using HangFire.Server.Performing;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class PerformContextFacts
    {
        private const string JobId = "id";

        private WorkerContext _workerContext;
        private Mock<IStorageConnection> _connection;
        private MethodData _methodData;

        public PerformContextFacts()
        {
            _workerContext = new WorkerContext("Server", new string[0], 1);
            _connection = new Mock<IStorageConnection>();
            _methodData = MethodData.FromExpression(() => Method());
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenWorkerContextIsNull()
        {
            Assert.Throws<NullReferenceException>(
                () => new PerformContext(null, _connection.Object, JobId, _methodData));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new PerformContext(_workerContext, null, JobId, _methodData));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new PerformContext(_workerContext, _connection.Object, null, _methodData));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodDataIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new PerformContext(_workerContext, _connection.Object, JobId, null));

            Assert.Equal("methodData", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var context = CreateContext();

            Assert.NotNull(context.Items);
            Assert.Same(_connection.Object, context.Connection);
            Assert.Equal(JobId, context.JobId);
            Assert.Same(_methodData, context.MethodData);
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

            Assert.Same(context.Items, contextCopy.Items);
            Assert.Same(context.Connection, contextCopy.Connection);
            Assert.Same(context.JobId, contextCopy.JobId);
            Assert.Same(context.MethodData, contextCopy.MethodData);
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
            return new PerformContext(_workerContext, _connection.Object, JobId, _methodData);
        }

        public static void Method() { }
    }
}
