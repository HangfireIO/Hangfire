using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Server
{
    public class ServerJobCancellationTokenFacts
    {
        private const string JobId = "my-job";
        private readonly Mock<IStorageConnection> _connection;
        private readonly StateData _stateData;
        private readonly BackgroundProcessContextMock _context;
        private readonly WorkerContextMock _workerContext;

        public ServerJobCancellationTokenFacts()
        {
            _stateData = new StateData
            {
                Name = ProcessingState.StateName,
                Data = new Dictionary<string, string>
                {
                    { "WorkerNumber", "1" },
                    { "ServerId", "Server" },
                }
            };

            _connection = new Mock<IStorageConnection>();
            _connection.Setup(x => x.GetStateData(JobId)).Returns(_stateData);

            _context = new BackgroundProcessContextMock();
            _context.ServerId = "Server";

            _workerContext = new WorkerContextMock { WorkerNumber = 1 };
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIsIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    null, _connection.Object, _workerContext.Object, _context.Object));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    JobId, null, _workerContext.Object, _context.Object));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenWorkerContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    JobId, _connection.Object, null, _context.Object));

            Assert.Equal("workerContext", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProcessContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    JobId, _connection.Object, _workerContext.Object, null));

            Assert.Equal("backgroundProcessContext", exception.ParamName);
        }

        [Fact]
        public void ShutdownTokenProperty_PointsToShutdownTokenValue()
        {
            var token = CreateToken();
            Assert.Equal(_context.CancellationTokenSource.Token, token.ShutdownToken);
        }

        [Fact]
        public void ThrowIfCancellationRequested_DoesNotThrowOnProcessingJob_IfNoShutdownRequested()
        {
            var token = CreateToken();

            Assert.DoesNotThrow(token.ThrowIfCancellationRequested);
        }

        [Fact]
        public void ThrowIfCancellationRequested_ThrowsOperationCanceled_OnShutdownRequest()
        {
            _context.CancellationTokenSource.Cancel();
            var token = CreateToken();

            Assert.Throws<OperationCanceledException>(
                () => token.ThrowIfCancellationRequested());
        }

        [Fact]
        public void ThrowIfCancellationRequested_Throws_IfStateDataDoesNotExist()
        {
            _connection.Setup(x => x.GetStateData(It.IsAny<string>())).Returns((StateData)null);
            var token = CreateToken();

            Assert.Throws<JobAbortedException>(() => token.ThrowIfCancellationRequested());
        }

        [Fact]
        public void ThrowIfCancellationRequested_ThrowsJobAborted_IfJobIsNotInProcessingState()
        {
            _stateData.Name = "NotProcessing";
            var token = CreateToken();

            Assert.Throws<JobAbortedException>(
                () => token.ThrowIfCancellationRequested());
        }

        [Fact]
        public void ThrowIfCancellationRequested_ThrowsJobAborted_IfStateData_ContainsDifferentServerId()
        {
            _stateData.Data["ServerId"] = "AnotherServer";
            var token = CreateToken();

            Assert.Throws<JobAbortedException>(
                () => token.ThrowIfCancellationRequested());
        }

        [Fact]
        public void ThrowIfCancellationRequested_ThrowsJobAborted_IfWorkerNumberWasChanged()
        {
            _stateData.Data["WorkerNumber"] = "999";
            var token = CreateToken();

            Assert.Throws<JobAbortedException>(
                () => token.ThrowIfCancellationRequested());
        }

        private IJobCancellationToken CreateToken()
        {
            return new ServerJobCancellationToken(
                JobId, _connection.Object, _workerContext.Object, _context.Object);
        }
    }
}
