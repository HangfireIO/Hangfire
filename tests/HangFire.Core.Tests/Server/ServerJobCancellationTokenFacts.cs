using System;
using System.Collections.Generic;
using System.Threading;
using HangFire.Server;
using HangFire.States;
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
        private readonly WorkerContextMock _workerContextMock;
        private readonly StateData _stateData;

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

            _workerContextMock = new WorkerContextMock { WorkerNumber = 1 };
            _workerContextMock.SharedContext.ServerId = "Server";

            _shutdownToken = new CancellationToken(false);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIsIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    null, _connection.Object, _workerContextMock.Object, new CancellationToken()));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    JobId, null, _workerContextMock.Object, new CancellationToken()));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenWorkerContextIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    JobId, _connection.Object, null, new CancellationToken()));

            Assert.Equal("workerContext", exception.ParamName);
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
            _shutdownToken = new CancellationToken(true);
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
                JobId, _connection.Object, _workerContextMock.Object, _shutdownToken);
        }
    }
}
