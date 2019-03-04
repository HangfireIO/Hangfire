using System;
using Hangfire.Common;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class AwaitingStateFacts
    {
        [Fact]
        public void StateName_IsEqualToAwaiting()
        {
            Assert.Equal("Awaiting", AwaitingState.StateName);
        }

        [Fact]
        public void NameProperty_ReturnsStateName()
        {
            var state = CreateState();
            Assert.Equal(AwaitingState.StateName, state.Name);
        }

        [Fact]
        public void SerializeData_ReturnsCorrectData()
        {
            var state = CreateState();

            var data = state.SerializeData();

            Assert.Equal(state.ParentId, data["ParentId"]);
            Assert.Matches(
                "^{\"\\$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\"}$",
                data["NextState"]);
            Assert.Equal(state.Options.ToString("D"), data["Options"]);
            Assert.Equal(state.Expiration.ToString(), data["Expiration"]);
        }

        [Fact]
        public void IsFinal_ReturnsFalse()
        {
            var state = CreateState();
            Assert.False(state.IsFinal);
        }

        [Fact]
        public void IgnoreExceptions_ReturnsFalse()
        {
            var state = CreateState();
            Assert.False(state.IgnoreJobLoadException);
        }

        [Fact, CleanSerializerSettings]
        public void SerializeData_HandlesChangingProcessOfInternalDataSerialization()
        {
            SerializationHelper.SetUserSerializerSettings(SerializerSettingsHelper.DangerousSettings);

            var nextStateSerialized = SerializationHelper.Serialize(new EnqueuedState(), SerializationOption.User);

            var nextState = SerializationHelper.Deserialize<IState>(nextStateSerialized, SerializationOption.TypedInternal) as EnqueuedState;
            Assert.NotNull(nextState);
            Assert.NotEqual(default(DateTime), nextState.EnqueuedAt);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void JsonSerialize_ReturnsEfficientString_AfterVersion170()
        {
            var state = new AwaitingState("parent");

            var serialized = SerializationHelper.Serialize(state, SerializationOption.TypedInternal);

            Assert.Equal(
                "{\"$type\":\"Hangfire.States.AwaitingState, Hangfire.Core\",\"ParentId\":\"parent\",\"NextState\":{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\"}}",
                serialized);
        }

        [DataCompatibilityRangeFact]
        public void JsonDeserialize_CanHandlePreviousFormat()
        {
            var json = "{\"$type\":\"Hangfire.States.AwaitingState, Hangfire.Core\",\"ParentId\":\"parent\",\"NextState\":{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\"},\"Options\":1,\"Name\":\"Awaiting\"}";
            var state = SerializationHelper.Deserialize<AwaitingState>(json, SerializationOption.TypedInternal);

            Assert.Equal("parent", state.ParentId);
            Assert.Equal("Enqueued", state.NextState.Name);
        }

        [DataCompatibilityRangeFact]
        public void JsonDeserialize_CanHandleNewFormat()
        {
            var json = "{\"$type\":\"Hangfire.States.AwaitingState, Hangfire.Core\",\"ParentId\":\"parent\",\"NextState\":{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\"}}";
            var state = SerializationHelper.Deserialize<AwaitingState>(json, SerializationOption.TypedInternal);

            Assert.Equal("parent", state.ParentId);
            Assert.Equal(null, state.Reason);
            Assert.Equal("Enqueued", state.NextState.Name);
        }

        private static AwaitingState CreateState()
        {
            return new AwaitingState("1", new EnqueuedState(), JobContinuationOptions.OnlyOnSucceededState, TimeSpan.FromDays(1));
        }
    }
}
