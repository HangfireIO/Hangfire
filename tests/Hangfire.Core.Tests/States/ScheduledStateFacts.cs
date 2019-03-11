using System;
using Hangfire.Common;
using Hangfire.States;
using Newtonsoft.Json;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class ScheduledStateFacts
    {
        [Fact]
        public void StateName_IsCorrect()
        {
            var state = new ScheduledState(DateTime.UtcNow);
            Assert.Equal(ScheduledState.StateName, state.Name);
        }

        [Fact]
        public void Ctor_SetsTheCorrectData_WhenDateIsPassed()
        {
            var date = new DateTime(2012, 12, 12);
            var state = new ScheduledState(date);
            Assert.Equal(date, state.EnqueueAt);
        }

        [Fact]
        public void Ctor_SetsTheCorrectDate_WhenTimeSpanIsPassed()
        {
            var state = new ScheduledState(TimeSpan.FromDays(1));
            Assert.True(DateTime.UtcNow.AddDays(1).AddMinutes(-1) < state.EnqueueAt);
            Assert.True(state.EnqueueAt < DateTime.UtcNow.AddDays(1).AddMinutes(1));
        }

        [Fact]
        public void SerializeData_ReturnsCorrectData()
        {
            var state = new ScheduledState(new DateTime(2012, 12, 12));

            var data = state.SerializeData();

            Assert.Equal(JobHelper.SerializeDateTime(state.EnqueueAt), data["EnqueueAt"]);
            Assert.Equal(JobHelper.SerializeDateTime(state.ScheduledAt), data["ScheduledAt"]);
        }

        [Fact]
        public void IsFinal_ReturnsFalse()
        {
            var state = new ScheduledState(DateTime.UtcNow);

            Assert.False(state.IsFinal);
        }

        [Fact]
        public void IgnoreExceptions_ReturnsFalse()
        {
            var state = new ScheduledState(DateTime.UtcNow);
            Assert.False(state.IgnoreJobLoadException);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170)]
        public void JsonSerialize_ReturnsCorrectString_Before170()
        {
            var dateTime = DateTime.UtcNow;
            var state = new ScheduledState(dateTime);
            var convertedDateTime = JsonConvert.SerializeObject(dateTime);

            var serialized = SerializationHelper.Serialize<IState>(state, SerializationOption.TypedInternal);

            Assert.Equal(
                "{\"$type\":\"Hangfire.States.ScheduledState, Hangfire.Core\",\"EnqueueAt\":" + convertedDateTime + ",\"Reason\":null}",
                serialized);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void JsonSerialize_ReturnsCorrectString_After170()
        {
            var dateTime = DateTime.UtcNow;
            var state = new ScheduledState(dateTime);
            var convertedDateTime = JsonConvert.SerializeObject(dateTime);

            var serialized = SerializationHelper.Serialize<IState>(state, SerializationOption.TypedInternal);

            Assert.Equal(
                "{\"$type\":\"Hangfire.States.ScheduledState, Hangfire.Core\",\"EnqueueAt\":" + convertedDateTime + "}",
                serialized);
        }
    }
}
