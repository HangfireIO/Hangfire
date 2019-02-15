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
    public class ServerJobCancellationTokenFacts
    {
        private const string ServerId = "some-server";
        private const string WorkerId = "1";
        private const string JobId = "my-job";
        private readonly Mock<IStorageConnection> _connection;
        private readonly StateData _stateData;
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
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    null, JobId, ServerId, WorkerId, _cts.Token));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    _connection.Object, null, ServerId, WorkerId, _cts.Token));

            Assert.Equal("jobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenServerIdIsIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    _connection.Object, JobId, null, WorkerId, _cts.Token));

            Assert.Equal("serverId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenWorkerIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new ServerJobCancellationToken(
                    _connection.Object, JobId, ServerId, null, _cts.Token));

            Assert.Equal("workerId", exception.ParamName);
        }

        [Fact]
        public void ShutdownTokenProperty_PointsToValue_LinkedWithShutdownToken()
        {
            var token = CreateToken();
            Assert.False(token.ShutdownToken.IsCancellationRequested);
            _cts.Cancel();
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
            _cts.Cancel();
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
        public void CancellationToken_IsInitializedAsNotCancelled_IfNotAborted()
        {
            var token = CreateToken();
            
            Assert.False(token.ShutdownToken.IsCancellationRequested);
        }

        [Fact]
        public void CancellationToken_IsInitializedAsCancelled_IfAborted()
        {
            var token = CreateToken();

            token.Abort();

            Assert.True(token.ShutdownToken.IsCancellationRequested);
        }

        [Fact]
        public void CheckAllCancellationTokens_DoesNotAbortCancellationToken_IfNothingChanged()
        {
            var token = CreateToken();

            ServerJobCancellationToken.CheckAllCancellationTokens(_connection.Object);
            
            Assert.False(token.IsAborted);
            token.ShutdownToken.ThrowIfCancellationRequested(); // does not throw
        }

        [Fact]
        public void CheckAllCancellationTokens_AbortsCancellationToken_IfStateDataDoesNotExist()
        {
            _connection.Setup(x => x.GetStateData(It.IsAny<string>())).Returns((StateData)null);
            var token = CreateToken();

            ServerJobCancellationToken.CheckAllCancellationTokens(_connection.Object);

            Assert.Throws<OperationCanceledException>(
                () => token.ShutdownToken.ThrowIfCancellationRequested());
            Assert.True(token.IsAborted);
        }
        
        [Fact]
        public void CheckAllCancellationTokens_AbortsCancellationToken_IfJobIsNotInProcessingState()
        {
            _stateData.Name = "NotProcessing";
            var token = CreateToken();

            ServerJobCancellationToken.CheckAllCancellationTokens(_connection.Object);

            Assert.Throws<OperationCanceledException>(
                () => token.ShutdownToken.ThrowIfCancellationRequested());
            Assert.True(token.IsAborted);
        }
        
        [Fact]
        public void CheckAllCancellationTokens_AbortsCancellationToken_IfServerIdWasChanged()
        {
            _stateData.Data["ServerId"] = "another-server";
            var token = CreateToken();

            ServerJobCancellationToken.CheckAllCancellationTokens(_connection.Object);

            Assert.Throws<OperationCanceledException>(
                () => token.ShutdownToken.ThrowIfCancellationRequested());
            Assert.True(token.IsAborted);
        }

        [Fact]
        public void CheckAllCancellationTokens_AbortsCancellationToken_IfWorkerIdWasChanged()
        {
            _stateData.Data["WorkerId"] = "999";
            var token = CreateToken();

            ServerJobCancellationToken.CheckAllCancellationTokens(_connection.Object);

            Assert.Throws<OperationCanceledException>(
                () => token.ShutdownToken.ThrowIfCancellationRequested());
            Assert.True(token.IsAborted);
        }

        private ServerJobCancellationToken CreateToken()
        {
            return new ServerJobCancellationToken(_connection.Object, JobId, ServerId, WorkerId, _cts.Token);
        }
    }
}
