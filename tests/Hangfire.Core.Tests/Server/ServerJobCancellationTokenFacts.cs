using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Server
{
    public class ServerJobCancellationTokenFacts : IDisposable
    {
        private const string ServerId = "some-server";
        private const string WorkerId = "1";
        private const string JobId = "my-job";
        private readonly Mock<IStorageConnection> _connection;
        private readonly StateData _stateData;
        private readonly CancellationTokenSource _shutdownCts;
        private readonly CancellationTokenSource _cts;

        public ServerJobCancellationTokenFacts()
        {
            _stateData = new StateData
            {
                Name = ProcessingState.StateName,
                Data = new Dictionary<string, string>
                {
                    { "ServerId", ServerId },
                    { "WorkerId", WorkerId },
                }
            };

            _connection = new Mock<IStorageConnection>();
            _connection.Setup(x => x.GetStateData(JobId)).Returns(_stateData);

            _cts = new CancellationTokenSource();
            _shutdownCts = new CancellationTokenSource();

            ServerJobCancellationToken.AddServer(ServerId);
        }

        public void Dispose()
        {
            ServerJobCancellationToken.RemoveServer(ServerId);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    null, JobId, ServerId, WorkerId, _shutdownCts.Token));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    _connection.Object, null, ServerId, WorkerId, _shutdownCts.Token));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerIdIsIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    _connection.Object, JobId, null, WorkerId, _shutdownCts.Token));

            Assert.Equal("serverId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenWorkerIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    _connection.Object, JobId, ServerId, null, _shutdownCts.Token));

            Assert.Equal("workerId", exception.ParamName);
        }

        [Fact]
        public void ShutdownTokenProperty_PointsToValue_LinkedWithShutdownToken()
        {
            var token = CreateToken();
            Assert.False(token.ShutdownToken.IsCancellationRequested);
            _shutdownCts.Cancel();
            Assert.True(token.ShutdownToken.IsCancellationRequested);
        }

        [Fact]
        public void ThrowIfCancellationRequested_DoesNotThrowOnProcessingJob_IfNoShutdownRequested()
        {
            var token = CreateToken();

            // Does not throw
            token.ThrowIfCancellationRequested();
        }

        [Fact]
        public void ThrowIfCancellationRequested_ThrowsOperationCanceled_OnShutdownRequest()
        {
            _shutdownCts.Cancel();
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
        public void ThrowIfCancellationRequested_ThrowsJobAborted_IfServerIdWasChanged()
        {
            _stateData.Data["ServerId"] = "another-server";
            var token = CreateToken();

            Assert.Throws<JobAbortedException>(
                () => token.ThrowIfCancellationRequested());
        }

        [Fact]
        public void ThrowIfCancellationRequested_ThrowsJobAborted_IfWorkerIdWasChanged()
        {
            _stateData.Data["WorkerId"] = "999";
            var token = CreateToken();

            Assert.Throws<JobAbortedException>(
                () => token.ThrowIfCancellationRequested());
        }

        [Fact]
        public void CancellationToken_IsInitializedAsNotCancelled()
        {
            var token = CreateToken();
            
            Assert.False(token.ShutdownToken.IsCancellationRequested);
        }

        [Fact]
        public void CheckAllCancellationTokens_DoesNotAbortCancellationToken_IfNothingChanged()
        {
            var token = CreateToken();

            Assert.False(token.ShutdownToken.IsCancellationRequested);
            ServerJobCancellationToken.CheckAllCancellationTokens(ServerId, _connection.Object, _cts.Token);

            Assert.False(token.IsAborted);
            token.ShutdownToken.ThrowIfCancellationRequested(); // does not throw
        }

        [Fact]
        public void CheckAllCancellationTokens_AbortsCancellationToken_IfStateDataDoesNotExist()
        {
            _connection.Setup(x => x.GetStateData(It.IsAny<string>())).Returns((StateData)null);
            var token = CreateToken();

            Assert.False(token.ShutdownToken.IsCancellationRequested);
            ServerJobCancellationToken.CheckAllCancellationTokens(ServerId, _connection.Object, _cts.Token);

            Assert.Throws<OperationCanceledException>(
                () => token.ShutdownToken.ThrowIfCancellationRequested());
            Assert.True(token.IsAborted);
        }
        
        [Fact]
        public void CheckAllCancellationTokens_AbortsCancellationToken_IfJobIsNotInProcessingState()
        {
            _stateData.Name = "NotProcessing";
            var token = CreateToken();

            Assert.False(token.ShutdownToken.IsCancellationRequested);
            ServerJobCancellationToken.CheckAllCancellationTokens(ServerId, _connection.Object, _cts.Token);

            Assert.Throws<OperationCanceledException>(
                () => token.ShutdownToken.ThrowIfCancellationRequested());
            Assert.True(token.IsAborted);
        }
        
        [Fact]
        public void CheckAllCancellationTokens_AbortsCancellationToken_IfServerIdWasChanged()
        {
            _stateData.Data["ServerId"] = "another-server";
            var token = CreateToken();

            Assert.False(token.ShutdownToken.IsCancellationRequested);
            ServerJobCancellationToken.CheckAllCancellationTokens(ServerId, _connection.Object, _cts.Token);

            Assert.Throws<OperationCanceledException>(
                () => token.ShutdownToken.ThrowIfCancellationRequested());
            Assert.True(token.IsAborted);
        }

        [Fact]
        public void CheckAllCancellationTokens_AbortsCancellationToken_IfWorkerIdWasChanged()
        {
            _stateData.Data["WorkerId"] = "999";
            var token = CreateToken();

            Assert.False(token.ShutdownToken.IsCancellationRequested);
            ServerJobCancellationToken.CheckAllCancellationTokens(ServerId, _connection.Object, _cts.Token);

            Assert.Throws<OperationCanceledException>(
                () => token.ShutdownToken.ThrowIfCancellationRequested());
            Assert.True(token.IsAborted);
        }

        [Fact]
        public void CheckAllCancellationTokens_DoesNotAbortJobsFromOtherServers()
        {
            _stateData.Name = "NotProcessing";
            var token = CreateToken();

            Assert.False(token.ShutdownToken.IsCancellationRequested);
            ServerJobCancellationToken.CheckAllCancellationTokens("another-id", _connection.Object, _cts.Token);

            token.ShutdownToken.ThrowIfCancellationRequested();
            Assert.False(token.IsAborted);
        }

        [Fact]
        public void CheckAllCancellationTokens_DoesNotPerformChecks_WhenShutdownTokenWasNotInitialized()
        {
            _stateData.Name = "NotProcessing";
            var token = CreateToken();

            ServerJobCancellationToken.CheckAllCancellationTokens(ServerId, _connection.Object, _cts.Token);

            Assert.False(token.IsAborted);
            _connection.Verify(x => x.GetStateData(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void CheckAllCancellationTokens_DoesNotPerformChecks_WhenJobIsAlreadyAborted()
        {
            _stateData.Name = "NotProcessing";
            var token = CreateToken();

            Assert.False(token.ShutdownToken.IsCancellationRequested);

            ServerJobCancellationToken.CheckAllCancellationTokens(ServerId, _connection.Object, _cts.Token);
            Assert.True(token.IsAborted);

            ServerJobCancellationToken.CheckAllCancellationTokens(ServerId, _connection.Object, _cts.Token);

            Assert.True(token.IsAborted);
            _connection.Verify(x => x.GetStateData(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void CheckAllCancellationTokens_PerformsAdditionalChecks_WhenPriorOnesDidNotLeadToAbort()
        {
            var token = CreateToken();
            Assert.False(token.ShutdownToken.IsCancellationRequested);

            ServerJobCancellationToken.CheckAllCancellationTokens(ServerId, _connection.Object, _cts.Token);
            Assert.False(token.IsAborted);

            _stateData.Name = "NotProcessing";
            ServerJobCancellationToken.CheckAllCancellationTokens(ServerId, _connection.Object, _cts.Token);

            Assert.True(token.IsAborted);
            _connection.Verify(x => x.GetStateData(It.IsAny<string>()), Times.Exactly(2));
        }

        private ServerJobCancellationToken CreateToken(string serverId = null)
        {
            return new ServerJobCancellationToken(_connection.Object, JobId, serverId ?? ServerId, WorkerId, _shutdownCts.Token);
        }
    }
}
