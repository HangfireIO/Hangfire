using System;
using System.Threading;
using HangFire.Server;
using HangFire.Storage;
using Moq;
using Xunit;

namespace HangFire.Core.Tests.Server
{
    public class ServerJobCancellationTokenFacts
    {
        private const string JobId = "my-job";
        private readonly Mock<IStorageConnection> _connection;
        private CancellationToken _shutdownToken;

        public ServerJobCancellationTokenFacts()
        {
            _connection = new Mock<IStorageConnection>();
            _shutdownToken = new CancellationToken(false);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIsIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(null, _connection.Object, new CancellationToken()));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(JobId, null, new CancellationToken()));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void IsCancellationRequested_ReturnsTrue_WithoutStorageAccess_IfItWasRequestedByShutdown()
        {
            _shutdownToken = new CancellationToken(true);
            var token = CreateToken();

            var result = token.IsCancellationRequested;

            Assert.True(result);
            _connection.Verify(
                x => x.GetJobParameter(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public void IsCancellationRequested_ReturnsTrue_IfAJobWasAborted()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "AbortRequested")).Returns("true");
            var token = CreateToken();

            var result = token.IsCancellationRequested;

            Assert.True(result);
        }

        [Fact]
        public void IsCancellationRequested_ReturnsFalse_IfAbortedParameterIsNull()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "AbortRequested")).Returns((string)null);
            var token = CreateToken();

            var result = token.IsCancellationRequested;

            Assert.False(result);
        }

        [Fact]
        public void IsCancellationRequested_ReturnsFalse_IfAbortedParameterIsFalse()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "AbortRequested")).Returns("false");
            var token = CreateToken();

            var result = token.IsCancellationRequested;

            Assert.False(result);
        }

        [Fact]
        public void ThrowIfCancellationRequested_DoesNotThrow_IfNeitherShutdownNorAbortRequested()
        {
            var token = CreateToken();

            Assert.DoesNotThrow(token.ThrowIfCancellationRequested);
        }

        [Fact]
        public void ThrowIfCancellationRequested_ThrowsOperationCanceled_OnShutdownRequest()
        {
            _shutdownToken = new CancellationToken(true);
            var token = CreateToken();

            Assert.Throws<OperationCanceledException>(
                () => token.ThrowIfCancellationRequested());
        }

        [Fact]
        public void ThrowIfCancellationRequested_ThrowsJobAborted_OnAbort()
        {
            _connection.Setup(x => x.GetJobParameter(JobId, "AbortRequested")).Returns("true");
            var token = CreateToken();

            Assert.Throws<JobAbortedException>(
                () => token.ThrowIfCancellationRequested());
        }

        private IJobCancellationToken CreateToken()
        {
            return new ServerJobCancellationToken(JobId, _connection.Object, _shutdownToken);
        }
    }
}
