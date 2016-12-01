using System;
using System.Runtime.Serialization.Formatters;
using Hangfire.Common;
using Hangfire.States;
using Newtonsoft.Json;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class AwaitingStateFacts
    {
        [Fact, CleanJsonSerializersSettings]
        public void SerializeData_HandlesChangingCoreSerializerSettings()
        {
            var previousSerializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Full,

                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,

                Formatting = Formatting.Indented,

                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            JobHelper.SetSerializerSettings(previousSerializerSettings);

            var nextStateSerialized = JobHelper.ToJson(new EnqueuedState());
            var nextState = JobHelper.Deserialize<IState>(nextStateSerialized, TypeNameHandling.Objects) as EnqueuedState;
            Assert.NotNull(nextState);
            Assert.NotEqual(default(DateTime), nextState.EnqueuedAt);
        }
    }
}
