using System;
using Hangfire.Common;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class EnqueuedStateFacts
    {
        [Fact]
        public void StateName_IsCorrect()
        {
            var state = new EnqueuedState();
            Assert.Equal(EnqueuedState.StateName, state.Name);
        }

        [Fact]
        public void Ctor_ShouldSetQueue_WhenItWasGiven()
        {
            var state = new EnqueuedState("critical");
            Assert.Equal("critical", state.Queue);
        }

        [Fact]
        public void SetQueue_ThrowsAnException_WhenQueueValueIsEmpty()
        {
            var state = new EnqueuedState();
            Assert.Throws<ArgumentNullException>(() => state.Queue = String.Empty);
        }

        [Fact]
        public void SetQueue_ThrowsAnException_WhenValueIsNotInAGivenFormat()
        {
            var state = new EnqueuedState();

            Assert.Throws<ArgumentException>(() => state.Queue = "UppercaseLetters");
            Assert.Throws<ArgumentException>(() => state.Queue = "punctuation:un-allowed");
            Assert.Throws<ArgumentException>(() => state.Queue = "моя_твоя_непонимать");
        }

        [Fact]
        public void SetQueue_DoesNotThrowException_WhenValueIsInACorrectFormat()
        {
            var state = new EnqueuedState();

            // Does not throw
            state.Queue = "lowercasedcharacters";
            state.Queue = "underscores_allowed";
            state.Queue = "1234567890_allowed";
        }

        [Fact]
        public void SerializeData_ReturnsCorrectData()
        {
            var state = new EnqueuedState();

            var serializedData = state.SerializeData();

            Assert.Equal(state.Queue, serializedData["Queue"]);
            Assert.Equal(JobHelper.SerializeDateTime(state.EnqueuedAt), serializedData["EnqueuedAt"]);
        }

        [Fact]
        public void IsFinal_ReturnsFalse()
        {
            var state = new EnqueuedState();

            Assert.False(state.IsFinal);
        }

        [Fact]
        public void IgnoreExceptions_ReturnsFalse()
        {
            var state = new EnqueuedState();

            Assert.False(state.IgnoreJobLoadException);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170)]
        public void JsonSerialize_ReturnsCorrectString_Before170()
        {
            var state = new EnqueuedState("default");

            var serialized = SerializationHelper.Serialize<IState>(state, SerializationOption.TypedInternal);

            Assert.Equal(
                "{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\",\"Queue\":\"default\",\"Reason\":null}",
                serialized);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void JsonSerialize_ReturnsCorrectString_After170()
        {
            var state = new EnqueuedState("default");

            var serialized = SerializationHelper.Serialize<IState>(state, SerializationOption.TypedInternal);

            Assert.Equal(
                "{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\",\"Queue\":\"default\"}",
                serialized);
        }

        [DataCompatibilityRangeFact]
        public void JsonDeserialize_CanHandlePreviousFormat()
        {
            var json = "{\"Queue\":\"critical\",\"EnqueuedAt\":\"2012-04-02T11:22:33.0000000Z\",\"Name\":\"Enqueued\",\"Reason\":\"hello\",\"IsFinal\":false,\"IgnoreJobLoadException\":false}";
            var state = SerializationHelper.Deserialize<EnqueuedState>(json);

            Assert.Equal("critical", state.Queue);
            Assert.Equal("hello", state.Reason);
        }

        [DataCompatibilityRangeFact]
        public void JsonDeserialize_CanHandleNewFormat()
        {
            var json = "{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\",\"Queue\":\"default\"}";
            var state = SerializationHelper.Deserialize<EnqueuedState>(json);

            Assert.Equal("default", state.Queue);
            Assert.Equal(null, state.Reason);
        }

        [DataCompatibilityRangeFact]
        public void JsonDeserialize_CanHandle170Beta1Format_WithDefaultValueForQueue()
        {
            var json = "{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\"}";
            var state = SerializationHelper.Deserialize<EnqueuedState>(json);

            Assert.Equal("default", state.Queue);
            Assert.Equal(null, state.Reason);
        }
    }
}
