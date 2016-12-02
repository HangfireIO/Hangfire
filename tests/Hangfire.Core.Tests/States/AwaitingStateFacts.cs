using System;
using Hangfire.Common;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class AwaitingStateFacts
    {
        [Fact, CleanJsonSerializersSettings]
        public void SerializeData_HandlesChangingProcessOfInternalDataSerialization()
        {
            SerializationHelper.SetUserSerializerSettings(SerializerSettingsHelper.DangerousSettings);

            var nextStateSerialized = SerializationHelper.Serialize(new EnqueuedState(), SerializationOption.User);

            var nextState = SerializationHelper.Deserialize<IState>(nextStateSerialized, SerializationOption.DefaultWithTypes) as EnqueuedState;
            Assert.NotNull(nextState);
            Assert.NotEqual(default(DateTime), nextState.EnqueuedAt);
        }
    }
}
