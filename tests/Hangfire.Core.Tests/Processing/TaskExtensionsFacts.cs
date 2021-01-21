using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using TaskExtensions = Hangfire.Processing.TaskExtensions;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Processing
{
    public class TaskExtensionsFacts
    {
        private readonly ManualResetEvent _mre;
        private readonly CancellationTokenSource _cts;

        public TaskExtensionsFacts()
        {
            _mre = new ManualResetEvent(false);
            _cts = new CancellationTokenSource();
        }

        [Fact]
        public async Task WaitOneAsync_ThrowsArgNullException_WhenWaitHandleIsNull()
        {
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await TaskExtensions.WaitOneAsync(null, TimeSpan.Zero, CancellationToken.None));

            Assert.Equal("waitHandle", exception.ParamName);
        }

        [Fact]
        public async Task WaitOneAsync_ThrowsOpCanceledException_WhenCancellationTokenIsCanceled()
        {
            _cts.Cancel();

            var exception = await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await TaskExtensions.WaitOneAsync(_mre, TimeSpan.Zero, _cts.Token));

            Assert.Equal(_cts.Token, exception.CancellationToken);
        }

        [Fact]
        public async Task WaitOneAsync_ThrowsOpCanceledException_EvenWhenWaitHandleIsSignaled()
        {
            _cts.Cancel();
            _mre.Set();

            var exception = await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await TaskExtensions.WaitOneAsync(_mre, Timeout.InfiniteTimeSpan, _cts.Token));

            Assert.Equal(_cts.Token, exception.CancellationToken);
        }

        [Fact]
        public async Task WaitOneAsync_ReturnsTrue_WhenWaitHandleIsSignaled()
        {
            _mre.Set();

            var result = await TaskExtensions.WaitOneAsync(_mre, Timeout.InfiniteTimeSpan, _cts.Token);

            Assert.True(result);
        }

        [Fact]
        public async Task WaitOneAsync_ReturnsTrue_WhenWaitHandleIsSignaled_AndTimeoutIsZero()
        {
            _mre.Set();

            var result = await TaskExtensions.WaitOneAsync(_mre, TimeSpan.Zero, _cts.Token);

            Assert.True(result);
        }

        [Fact]
        public async Task WaitOneAsync_ReturnsFalseImmediately_WhenNotSignaled_AndTimeoutIsZero()
        {
             var result = await TaskExtensions.WaitOneAsync(_mre, TimeSpan.Zero, _cts.Token);

             Assert.False(result);
        }

        [Fact]
        public async Task WaitOneAsync_WaitsAndReturnsFalse_WhenNotSignaled_AndNonNullTimeout()
        {
            var sw = Stopwatch.StartNew();
            var result = await TaskExtensions.WaitOneAsync(_mre, TimeSpan.FromMilliseconds(100), _cts.Token);
            sw.Stop();

            Assert.False(result, "result != false");
            Assert.False(_cts.Token.IsCancellationRequested, "IsCancellationRequested != false");
            Assert.False(_mre.WaitOne(TimeSpan.Zero), "_mre is signaled");
            Assert.True(sw.Elapsed > TimeSpan.FromMilliseconds(95), sw.Elapsed.ToString());
        }

        [Fact]
        public async Task WaitOneAsync_WaitsAndThrowsTaskCanceled_WhenNotSignaled_AndCancellationTokenIsCanceled()
        {
            var sw = Stopwatch.StartNew();
            _cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await TaskExtensions.WaitOneAsync(_mre, Timeout.InfiniteTimeSpan, _cts.Token));
            sw.Stop();

#if !NET452
            Assert.Equal(_cts.Token, exception.CancellationToken);
#else
            Assert.NotNull(exception);
#endif
            Assert.True(sw.Elapsed > TimeSpan.FromMilliseconds(95), sw.Elapsed.ToString());
        }

        [Fact]
        public void WaitOne_ThrowsArgNullException_WhenWaitHandleIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => TaskExtensions.WaitOne(null, TimeSpan.Zero, CancellationToken.None));

            Assert.Equal("waitHandle", exception.ParamName);
        }

        [Fact]
        public void WaitOne_ThrowsOpCanceledException_WhenCancellationTokenIsCanceled()
        {
            _cts.Cancel();

            var exception = Assert.Throws<OperationCanceledException>(
                () => TaskExtensions.WaitOne(_mre, TimeSpan.Zero, _cts.Token));

            Assert.Equal(_cts.Token, exception.CancellationToken);
        }

        [Fact]
        public void WaitOne_ThrowsOpCanceledException_EvenWhenWaitHandleIsSignaled()
        {
            _cts.Cancel();
            _mre.Set();

            var exception = Assert.Throws<OperationCanceledException>(
                () => TaskExtensions.WaitOne(_mre, Timeout.InfiniteTimeSpan, _cts.Token));

            Assert.Equal(_cts.Token, exception.CancellationToken);
        }

        [Fact]
        public void WaitOne_ReturnsTrue_WhenWaitHandleIsSignaled()
        {
            _mre.Set();

            var result = TaskExtensions.WaitOne(_mre, Timeout.InfiniteTimeSpan, _cts.Token);

            Assert.True(result);
        }

        [Fact]
        public void WaitOne_ReturnsTrue_WhenWaitHandleIsSignaled_AndTimeoutIsZero()
        {
            _mre.Set();

            var result = TaskExtensions.WaitOne(_mre, TimeSpan.Zero, _cts.Token);

            Assert.True(result);
        }

        [Fact]
        public void WaitOne_ReturnsFalseImmediately_WhenNotSignaled_AndTimeoutIsZero()
        {
             var result = TaskExtensions.WaitOne(_mre, TimeSpan.Zero, CancellationToken.None);

             Assert.False(result);
        }

        [Fact]
        public void WaitOne_WaitsAndReturnsFalse_WhenNotSignaled_AndNonNullTimeout()
        {
            var sw = Stopwatch.StartNew();
            var result = TaskExtensions.WaitOne(_mre, TimeSpan.FromMilliseconds(100), CancellationToken.None);
            sw.Stop();

            Assert.False(result);
            Assert.True(sw.Elapsed > TimeSpan.FromMilliseconds(95), sw.Elapsed.ToString());
        }

        [Fact]
        public void WaitOne_WaitsAndThrowsTaskCanceled_WhenNotSignaled_AndCancellationTokenIsCanceled()
        {
            var sw = Stopwatch.StartNew();
            _cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            var exception = Assert.ThrowsAny<OperationCanceledException>(
                () => TaskExtensions.WaitOne(_mre, Timeout.InfiniteTimeSpan, _cts.Token));
            sw.Stop();

#if !NET452
            Assert.Equal(_cts.Token, exception.CancellationToken);
#else
            Assert.NotNull(exception);
#endif
            Assert.True(sw.Elapsed > TimeSpan.FromMilliseconds(95), sw.Elapsed.ToString());
        }
    }
}
