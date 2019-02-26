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

            var nextState = SerializationHelper.Deserialize<IState>(nextStateSerialized, SerializationOption.DefaultWithTypes) as EnqueuedState;
            Assert.NotNull(nextState);
            Assert.NotEqual(default(DateTime), nextState.EnqueuedAt);
        }

        private static AwaitingState CreateState()
        {
            return new AwaitingState("1", new EnqueuedState(), JobContinuationOptions.OnlyOnSucceededState, TimeSpan.FromDays(1));
        }
    }
}
