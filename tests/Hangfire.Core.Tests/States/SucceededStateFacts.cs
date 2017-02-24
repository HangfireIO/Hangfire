using System;
using System.Diagnostics;
using System.Threading;
using Hangfire.Common;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class SucceededStateFacts
    {
        [Fact]
        public void StateName_IsEqualToSucceeded()
        {
            Assert.Equal("Succeeded", SucceededState.StateName);
        }

        [Fact]
        public void NameProperty_ReturnsStateName()
        {
            var state = CreateState();
            Assert.Equal(SucceededState.StateName, state.Name);
        }

        [Fact]
        public void SerializeData_ReturnsCorrectData()
        {
            var state = CreateState();

            var data = state.SerializeData();

            Assert.Equal("\"Returned  value\"", data["Result"]);
            Assert.Equal(JobHelper.SerializeDateTime(state.SucceededAt), data["SucceededAt"]);
            Assert.Equal("123", data["PerformanceDuration"]);
            Assert.Equal("11", data["Latency"]);
        }

        [Fact]
        public void SerializeData_DoesNotAppendEntry_ForNullResult()
        {
            var state = new SucceededState(null, 0, 0);

            var data = state.SerializeData();

            Assert.False(data.ContainsKey("Result"));
        }

#if NETFULL
        [Fact]
        public void SerializeData_CorrectlyHandlesResults_ThatCantBeSerialized()
        {
            var process = new Process();
            var state = new SucceededState(process, 0, 0);

            var data = state.SerializeData();

            Assert.Contains("Can not serialize", data["Result"]);
        }
#endif

        [Fact]
        public void IsFinal_ReturnsTrue()
        {
            var state = CreateState();
            Assert.True(state.IsFinal);
        }

        [Fact]
        public void IgnoreExceptions_ReturnsFalse()
        {
            var state = CreateState();
            Assert.False(state.IgnoreJobLoadException);
        }

        private static SucceededState CreateState()
        {
            return new SucceededState("Returned  value", 11, 123);
        }
    }
}
