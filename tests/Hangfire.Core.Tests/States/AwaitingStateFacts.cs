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

        [DataCompatibilityRangeFact]
        public void SerializeData_ReturnsParentId_WithAnyCompatibilityLevel()
        {
            var state = CreateState();

            var data = state.SerializeData();

            Assert.Equal(state.ParentId, data["ParentId"]);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170)]
        public void SerializeData_ReturnsSerializedDefaultNextState_WithReason_ForVersionsBefore170()
        {
            var state = CreateState();

            var data = state.SerializeData();

            Assert.Equal("{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\",\"Queue\":\"default\",\"Reason\":null}", data["NextState"]);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170, MaxExcludingLevel = CompatibilityLevel.Version_190)]
        public void SerializeData_ReturnsSerializedDefaultNextState_FromVersion170_AndBelow190()
        {
            var state = CreateState();

            var data = state.SerializeData();

            Assert.Equal("{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\",\"Queue\":\"default\"}", data["NextState"]);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_190)]
        public void SerializeData_DoesNotReturnsSerializedDefaultNextState_FromVersion190()
        {
            var state = CreateState();

            var data = state.SerializeData();

            Assert.False(data.ContainsKey("NextState"));
        }

        [DataCompatibilityRangeFact]
        public void SerializeData_ReturnsSerializedNonDefaultEnqueuedNextState_ForAnyCompatibilityVersion()
        {
            var state = CreateState(nextState: new EnqueuedState("critical"));

            var data = state.SerializeData();

            Assert.StartsWith("{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\",\"Queue\":\"critical\"", data["NextState"]);
        }

        [DataCompatibilityRangeFact]
        public void SerializeData_ReturnsSerializedNonDefaultNextState_ForAnyCompatibilityVersion()
        {
            var state = CreateState(nextState: new DeletedState());

            var data = state.SerializeData();

            Assert.StartsWith("{\"$type\":\"Hangfire.States.DeletedState, Hangfire.Core\"", data["NextState"]);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170)]
        public void SerializeData_ReturnsOptions_InStringFormat_ForVersionsBefore170()
        {
            var state = CreateState(options: JobContinuationOptions.OnlyOnDeletedState);

            var data = state.SerializeData();

            Assert.Equal(state.Options.ToString("G"), data["Options"]);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void SerializeData_ReturnsOptions_InNumericFormat_ForVersions170AndAbove()
        {
            var state = CreateState(options: JobContinuationOptions.OnlyOnDeletedState);

            var data = state.SerializeData();
            
            Assert.Equal(state.Options.ToString("D"), data["Options"]);
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_190)]
        public void SerializeData_ReturnsDefaultOptions_ForVersionsBefore190()
        {
            var state = CreateState(options: JobContinuationOptions.OnlyOnSucceededState);

            var data = state.SerializeData();

            Assert.True(data.TryGetValue("Options", out var options) && !String.IsNullOrWhiteSpace(options));
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_190)]
        public void SerializeData_DoesNotReturnDefaultOptions_ForVersions190AndAbove()
        {
            var state = CreateState(options: JobContinuationOptions.OnlyOnSucceededState);

            var data = state.SerializeData();

            Assert.False(data.ContainsKey("Options"));
        }

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170)]
        public void SerializeData_ReturnsExpiration_ForVersionsBefore170()
        {
            var state = CreateState();

            var data = state.SerializeData();

            Assert.Equal(state.Expiration.ToString(), data["Expiration"]);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void SerializeData_DoesNotReturnExpiration_ForVersions170AndAbove()
        {
            var state = CreateState();

            var data = state.SerializeData();

            Assert.False(data.ContainsKey("Expiration"));
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

        [DataCompatibilityRangeFact(MaxExcludingLevel = CompatibilityLevel.Version_170)]
        public void JsonSerialize_ReturnsCorrectString_Before170()
        {
            var state = new AwaitingState("parent");

            var serialized = SerializationHelper.Serialize<IState>(state, SerializationOption.TypedInternal);

            Assert.Equal(
                "{\"$type\":\"Hangfire.States.AwaitingState, Hangfire.Core\",\"ParentId\":\"parent\",\"NextState\":{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\",\"Queue\":\"default\",\"Reason\":null},\"Options\":0,\"Reason\":null}",
                serialized);
        }

        [DataCompatibilityRangeFact(MinLevel = CompatibilityLevel.Version_170)]
        public void JsonSerialize_ReturnsCorrectString_After170()
        {
            var state = new AwaitingState("parent");

            var serialized = SerializationHelper.Serialize<IState>(state, SerializationOption.TypedInternal);

            Assert.Equal(
                "{\"$type\":\"Hangfire.States.AwaitingState, Hangfire.Core\",\"ParentId\":\"parent\",\"NextState\":{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\",\"Queue\":\"default\"}}",
                serialized);
        }

        [DataCompatibilityRangeFact]
        public void JsonDeserialize_CanHandlePreviousFormat()
        {
            var json = "{\"$type\":\"Hangfire.States.AwaitingState, Hangfire.Core\",\"ParentId\":\"parent\",\"NextState\":{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\",\"Queue\":\"default\"},\"Options\":1,\"Name\":\"Awaiting\"}";
            var state = SerializationHelper.Deserialize<AwaitingState>(json, SerializationOption.TypedInternal);

            Assert.Equal("parent", state.ParentId);
            Assert.Equal("Enqueued", state.NextState.Name);
        }

        [DataCompatibilityRangeFact]
        public void JsonDeserialize_CanHandleNewFormat()
        {
            var json = "{\"$type\":\"Hangfire.States.AwaitingState, Hangfire.Core\",\"ParentId\":\"parent\",\"NextState\":{\"$type\":\"Hangfire.States.EnqueuedState, Hangfire.Core\",\"Queue\":\"default\"}}";
            var state = SerializationHelper.Deserialize<AwaitingState>(json, SerializationOption.TypedInternal);

            Assert.Equal("parent", state.ParentId);
            Assert.Null(state.Reason);
            Assert.Equal("Enqueued", state.NextState.Name);
        }

        private static AwaitingState CreateState(IState nextState = null, JobContinuationOptions? options = null)
        {
            return new AwaitingState("1", nextState ?? new EnqueuedState(), options ?? JobContinuationOptions.OnlyOnSucceededState, TimeSpan.FromDays(1));
        }
    }
}
