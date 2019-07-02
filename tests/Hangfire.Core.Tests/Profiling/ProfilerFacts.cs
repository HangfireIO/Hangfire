using System;
using System.Collections.Generic;
using Hangfire.Logging;
using Hangfire.Profiling;
using Moq;
using Xunit;
// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.Profiling
{
    public class ProfilerFacts
    {
        private readonly Mock<ILog> _logger = new Mock<ILog>();
        private readonly object _instance = new object();

        public ProfilerFacts()
        {
            _logger.Setup(x => x.Log(It.IsAny<LogLevel>(), null, null)).Returns(true);
        }

        [Theory]
        [MemberData(nameof(GetProfilers))]
        internal void InvokeMeasured_ThrowsAnException_WhenActionIsNull(IProfiler profiler)
        {
            var exception = Assert.Throws<ArgumentNullException>(() => profiler.InvokeMeasured(
                _instance,
                null));

            Assert.Equal("action", exception.ParamName);
        }

        [Theory]
        [MemberData(nameof(GetProfilers))]
        internal void InvokeMeasured_ReturnsResult_ForFunctions(IProfiler profiler)
        {
            var result = profiler.InvokeMeasured(_instance, x =>
            {
                Assert.Same(_instance, x);
                return x.ToString();
            });

            Assert.Equal(_instance.ToString(), result);
        }

        [Theory]
        [MemberData(nameof(GetProfilers))]
        internal void InvokeMeasured_DoesNotThrowAnException_WhenInstanceIsNull(IProfiler profiler)
        {
            var result = profiler.InvokeMeasured((object)null, x =>
            {
                Assert.Null(x);
                return true;
            });

            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(GetProfilers))]
        internal void InvokeMeasured_WithAction_InvokesIt(IProfiler profiler)
        {
            var invoked = false;

            profiler.InvokeMeasured(_instance, x =>
            {
                Assert.Same(_instance, x);
                invoked = true;
            });

            Assert.True(invoked);
        }

        [Theory]
        [MemberData(nameof(GetProfilers))]
        internal void InvokeMeasured_WithActionAndNullInstance_InvokesIt(IProfiler profiler)
        {
            var invoked = false;

            profiler.InvokeMeasured((object)null, x =>
            {
                Assert.Null(x);
                invoked = true;
            });

            Assert.True(invoked);
        }

        [Fact]
        internal void SlowLog_GeneratesLogMessage_WhenThresholdReached_WithNullMessage()
        {
            var profiler = CreateSlowLogProfiler(_logger, TimeSpan.FromSeconds(-1));
            profiler.InvokeMeasured(_instance, x => x.ToString());

            _logger.Verify(x => x.Log(LogLevel.Warn, It.IsNotNull<Func<string>>(), null), Times.Once);
        }

        [Fact]
        internal void SlowLog_GeneratesLogMessage_WhenThresholdReached_WithNullInstance()
        {
            var profiler = CreateSlowLogProfiler(_logger, TimeSpan.FromSeconds(-1));
            profiler.InvokeMeasured((object)null, x => true);

            _logger.Verify(x => x.Log(LogLevel.Warn, It.IsNotNull<Func<string>>(), null), Times.Once);
        }

        [Fact]
        internal void SlowLog_GeneratesLogMessage_WhenThresholdReached_WithNonNullMessage()
        {
            var profiler = CreateSlowLogProfiler(_logger, TimeSpan.FromSeconds(-1));
            profiler.InvokeMeasured(_instance, x => x.ToString(), "message");

            _logger.Verify(x => x.Log(LogLevel.Warn, It.IsNotNull<Func<string>>(), null), Times.Once);
        }

        [Fact]
        internal void SlowLog_DoesNotGenerateLogMessage_WhenThresholdIsNotReached()
        {
            var profiler = CreateSlowLogProfiler(_logger, TimeSpan.FromSeconds(600));
            profiler.InvokeMeasured(_instance, x => x.ToString(), "message");

            _logger.Verify(x => x.Log(LogLevel.Warn, It.IsNotNull<Func<string>>(), null), Times.Never);
        }

        public static IEnumerable<object[]> GetProfilers()
        {
            yield return new object[] { EmptyProfiler.Instance };
            yield return new object[] { CreateSlowLogProfiler(new Mock<ILog>(), TimeSpan.FromSeconds(-1)) };
        }

        private static SlowLogProfiler CreateSlowLogProfiler(Mock<ILog> logger, TimeSpan threshold)
        {
            return new SlowLogProfiler(logger.Object, threshold);
        }
    }
}
