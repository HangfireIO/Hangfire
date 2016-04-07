using System;
using Hangfire.Server;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Server
{
    public class PerformContextFacts
    {
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IJobCancellationToken> _cancellationToken;
        private readonly BackgroundJobMock _backgroundJob;

        public PerformContextFacts()
        {
            _connection = new Mock<IStorageConnection>();
            _backgroundJob = new BackgroundJobMock();
            _cancellationToken = new Mock<IJobCancellationToken>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new PerformContext(null, _backgroundJob.Object, _cancellationToken.Object));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenBackgroundJobIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new PerformContext(_connection.Object, null, _cancellationToken.Object));

            Assert.Equal("backgroundJob", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenCancellationTokenIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new PerformContext(_connection.Object, _backgroundJob.Object, null));

            Assert.Equal("cancellationToken", exception.ParamName);
        }

        [Fact]
        public void Ctor_CorrectlySets_AllInstanceProperties()
        {
            var context = CreateContext();

            Assert.Equal(_backgroundJob.Object, context.BackgroundJob);
            Assert.NotNull(context.Items);
            Assert.Same(_connection.Object, context.Connection);
            Assert.Same(_cancellationToken.Object, context.CancellationToken);
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
            Assert.Same(context.BackgroundJob, contextCopy.BackgroundJob);
            Assert.Same(context.CancellationToken, contextCopy.CancellationToken);
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

            _connection.Verify(x => x.SetJobParameter(_backgroundJob.Id, "name", "\"value\""));
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
            _connection.Setup(x => x.GetJobParameter(_backgroundJob.Id, "name")).Returns("value");
            var context = CreateContext();

            Assert.Throws<InvalidOperationException>(
                () => context.GetJobParameter<int>("name"));
        }

        private PerformContext CreateContext()
        {
            return new PerformContext(
                _connection.Object, _backgroundJob.Object, _cancellationToken.Object);
        }

        public static void Method() { }
    }
}
