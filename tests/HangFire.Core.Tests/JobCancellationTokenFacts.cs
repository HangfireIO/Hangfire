using System;
using Xunit;

namespace HangFire.Core.Tests
{
    public class JobCancellationTokenFacts
    {
        [Fact]
        public void IsCancellationRequested_ReturnsTheCorrectValue()
        {
            var falseToken = new JobCancellationToken(false);
            var trueToken = new JobCancellationToken(true);

            Assert.False(falseToken.IsCancellationRequested);
            Assert.True(trueToken.IsCancellationRequested);
        }

        [Fact]
        public void ThrowIfCancellationRequested_DoesNotThrowOnFalseValue()
        {
            var token = new JobCancellationToken(false);

            Assert.DoesNotThrow(token.ThrowIfCancellationRequested);
        }

        [Fact]
        public void ThrowIfCancellationRequested_ThrowsOnTrueValue()
        {
            var token = new JobCancellationToken(true);

            Assert.Throws<OperationCanceledException>(
                () => token.ThrowIfCancellationRequested());
        }

        [Fact]
        public void Null_ReturnsNullValue()
        {
            Assert.Null(JobCancellationToken.Null);
        }
    }
}
