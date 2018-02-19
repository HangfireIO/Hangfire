using System;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class JobCancellationTokenFacts
    {
        [Fact]
        public void ShutdownToken_IsInCanceledState_WhenPassingTrueValue()
        {
            var token = new JobCancellationToken(true);
            Assert.True(token.ShutdownToken.IsCancellationRequested);
        }

        [Fact]
        public void ThrowIfCancellationRequested_DoesNotThrowOnFalseValue()
        {
            var token = new JobCancellationToken(false);

            // Does not throw
            token.ThrowIfCancellationRequested();
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
