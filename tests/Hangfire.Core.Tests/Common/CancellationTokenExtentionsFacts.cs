using System;
using System.Diagnostics;
using System.Threading;
using Hangfire.Common;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
    public class CancellationTokenExtentionsFacts
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        [Fact]
        public void GetCancellationEvent_ReturnsSomething()
        {
            var cancellationEvent = _cts.Token.GetCancellationEvent();

            Assert.NotNull(cancellationEvent);
            Assert.NotNull(cancellationEvent.WaitHandle);
        }

        [Fact]
        public void Wait_PerformsWait_NotLessThanTheSpecifiedTime()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = _cts.Token.Wait(TimeSpan.FromSeconds(1));
            stopwatch.Stop();

            Assert.False(result);
            Assert.True(stopwatch.Elapsed >= TimeSpan.FromMilliseconds(900), $"Elapsed: {stopwatch.Elapsed}");
        }

        [Fact]
        public void Wait_DoesNotPerformWait_WhenTokenIsCanceled()
        {
            _cts.Cancel();
            var stopwatch = Stopwatch.StartNew();
            var result = _cts.Token.Wait(TimeSpan.FromSeconds(1));
            stopwatch.Stop();

            Assert.True(result);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(900), $"Elapsed: {stopwatch.Elapsed}");
        }

        [Fact]
        public void WaitOrThrow_DoesNotThrow_WhenTokenIsNotCanceled()
        {
            _cts.Token.WaitOrThrow(TimeSpan.Zero);
            Assert.False(_cts.Token.IsCancellationRequested);
        }

        [Fact]
        public void WaitOrThrow_ThrowsAnException_WhenTokenIsCanceled()
        {
            _cts.Cancel();
            var exception = Assert.Throws<OperationCanceledException>(
                () => _cts.Token.WaitOrThrow(TimeSpan.FromSeconds(1)));

            Assert.Equal(_cts.Token, exception.CancellationToken);
            Assert.True(exception.CancellationToken.IsCancellationRequested);
        }
    }
}
