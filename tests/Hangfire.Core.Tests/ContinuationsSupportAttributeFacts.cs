using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using Hangfire.Common;
using Newtonsoft.Json;
using Xunit;
using static Hangfire.ContinuationsSupportAttribute;

namespace Hangfire.Core.Tests
{
    public class ContinuationsSupportAttributeFacts
    {
        [Fact, CleanJsonSerializersSettings]
        public void HandlesChangingCoreSerializerSettings()
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

            var continuationsJson = JobHelper.ToJson(new List<Continuation>
            {
                new Continuation {JobId = "1", Options = JobContinuationOptions.OnAnyFinishedState},
                new Continuation {JobId = "3214324", Options = JobContinuationOptions.OnlyOnSucceededState}
            });

            var continuations = JobHelper.Deserialize<List<Continuation>>(continuationsJson);

            Assert.Equal(2, continuations.Count);
            Assert.Equal("1", continuations[0].JobId);
            Assert.Equal(JobContinuationOptions.OnAnyFinishedState, continuations[0].Options);
            Assert.Equal("3214324", continuations[1].JobId);
            Assert.Equal(JobContinuationOptions.OnlyOnSucceededState, continuations[1].Options);
        }
    }
}
