using System;
using System.Diagnostics.CodeAnalysis;
using Hangfire.Common;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class DisableConcurrentExecutionAttributeFacts
    {
        private const int TimeoutInSeconds = 5;

        private readonly PerformContextMock _context;
        private readonly Mock<IDisposable> _distributedLock;

        public DisableConcurrentExecutionAttributeFacts()
        {
            _context = new PerformContextMock();
            _context.BackgroundJob.Job = Job.FromExpression(() => Sample());

            _distributedLock = new Mock<IDisposable>();
            _context.Connection
                .Setup(x => x.AcquireDistributedLock(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(_distributedLock.Object);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTimeoutInSecondsIsNegative()
        {
            Assert.Throws<ArgumentException>(
                () => new DisableConcurrentExecutionAttribute(-1));
        }

        [Fact]
        public void Ctor_CorrectlySets_TheTimeoutSecProperty()
        {
            var attribute = new DisableConcurrentExecutionAttribute(TimeoutInSeconds);

            Assert.Equal(TimeoutInSeconds, attribute.TimeoutSec);
        }

        [Fact]
        public void Ctor_CorrectlySets_BothResourceAndTimeoutSecProperties()
        {
            var attribute = new DisableConcurrentExecutionAttribute("my-resource", TimeoutInSeconds);

            Assert.Equal("my-resource", attribute.Resource);
            Assert.Equal(TimeoutInSeconds, attribute.TimeoutSec);
        }

        [Fact]
        public void OnPerforming_AcquiresDistributedLock_BasedOnJobTypeAndMethodName()
        {
            var attribute = CreateAttribute();

            attribute.OnPerforming(_context.GetPerformingContext());

            _context.Connection.Verify(x => x.AcquireDistributedLock(
                "DisableConcurrentExecutionAttributeFacts.Sample",
                TimeSpan.FromSeconds(TimeoutInSeconds)));
        }

        [Fact]
        public void OnPerforming_AcquiresDistributedLock_OnTheLowerCasedFormattedResource_WhenResourceIsSpecified()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => Sample("Some-Value"));
            var attribute = new DisableConcurrentExecutionAttribute("Prefix-{0}", TimeoutInSeconds);

            attribute.OnPerforming(_context.GetPerformingContext());

            _context.Connection.Verify(x => x.AcquireDistributedLock(
                "prefix-some-value",
                TimeSpan.FromSeconds(TimeoutInSeconds)));
        }

        [Fact]
        public void OnPerforming_ThrowsFormatException_WhenResourceFormatIsInvalid()
        {
            var attribute = new DisableConcurrentExecutionAttribute("resource-{1}", TimeoutInSeconds);

            Assert.Throws<FormatException>(
                () => attribute.OnPerforming(_context.GetPerformingContext()));
        }

        [Fact]
        public void OnPerforming_StoresAcquiredDistributedLock_InTheItemsCollection()
        {
            var attribute = CreateAttribute();
            var performingContext = _context.GetPerformingContext();

            attribute.OnPerforming(performingContext);

            Assert.Same(_distributedLock.Object, performingContext.Items["DistributedLock"]);
        }

        [Fact]
        public void OnPerformed_ReleasesTheAcquiredDistributedLock()
        {
            var attribute = CreateAttribute();
            attribute.OnPerforming(_context.GetPerformingContext());

            attribute.OnPerformed(_context.GetPerformedContext());

            _distributedLock.Verify(x => x.Dispose());
        }

        [Fact]
        public void OnPerformed_ThrowsAnException_WhenDistributedLockWasNotAcquired()
        {
            var attribute = CreateAttribute();

            Assert.Throws<InvalidOperationException>(
                () => attribute.OnPerformed(_context.GetPerformedContext()));
        }

        private static DisableConcurrentExecutionAttribute CreateAttribute()
        {
            return new DisableConcurrentExecutionAttribute(TimeoutInSeconds);
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void Sample()
        {
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void Sample(string value)
        {
        }
    }
}
