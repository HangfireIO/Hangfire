using System;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class DynamicMutexFacts : IDisposable
    {
        private readonly DynamicMutex<string> _mutex = new DynamicMutex<string>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        [Fact]
        public void Wait_ReturnsImmediately_WhenResourceWasNotHold()
        {
            var sw = Stopwatch.StartNew();
            _mutex.Wait("hello", _cts.Token, out var acquired);
            sw.Stop();

            Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1));
            Assert.True(acquired);
        }

        [Fact]
        public void Wait_FailsOnCancellationToken_WhenResourceAlreadyAcquired()
        {
            _cts.CancelAfter(TimeSpan.FromSeconds(1));
            _mutex.Wait("hello", _cts.Token, out _);

            var acquired = false;
            var sw = Stopwatch.StartNew();
            Assert.Throws<OperationCanceledException>(
                () => _mutex.Wait("hello", _cts.Token, out acquired));
            sw.Stop();

            Assert.True(sw.Elapsed > TimeSpan.FromMilliseconds(500));
            Assert.False(acquired);
        }

        [Fact]
        public void Release_ThrowsWhenResourceWasNotAcquiredFirst()
        {
            Assert.Throws<InvalidOperationException>(() => _mutex.Release("hello"));
        }

        [Fact]
        public void Release_MakesItPossibleToAcquireTheResourceAgain()
        {
            _mutex.Wait("hello", _cts.Token, out _);
            _mutex.Release("hello");

            var sw = Stopwatch.StartNew();
            _mutex.Wait("hello", _cts.Token, out var acquired);
            sw.Stop();

            Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1));
            Assert.True(acquired);
        }

        [Fact]
        public void Release_CanFulfillOutstandingWaitRequest()
        {
            using (var timer = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
            using (timer.Token.Register(() => _mutex.Release("hello")))
            {
                _mutex.Wait("hello", _cts.Token, out _);
                
                var sw = Stopwatch.StartNew();
                _mutex.Wait("hello", _cts.Token, out var acquired);
                sw.Stop();

                Assert.True(TimeSpan.FromMilliseconds(500) < sw.Elapsed);
                Assert.True(sw.Elapsed < TimeSpan.FromSeconds(2));
                Assert.True(acquired);
            }
        }

        public void Dispose()
        {
            _cts.Dispose();
        }
    }
}