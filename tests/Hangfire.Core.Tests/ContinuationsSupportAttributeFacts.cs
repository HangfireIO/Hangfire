using System.Collections.Generic;
using Hangfire.Common;
using Xunit;
using static Hangfire.ContinuationsSupportAttribute;

namespace Hangfire.Core.Tests
{
    public class ContinuationsSupportAttributeFacts
    {
        [Fact, CleanJsonSerializersSettings]
        public void HandlesChangingProcessOfInternalDataSerialization()
        {
            SerializationHelper.SetUserSerializerSettings(SerializerSettingsHelper.DangerousSettings);

            var continuationsJson = SerializationHelper.Serialize(new List<Continuation>
            {
                new Continuation {JobId = "1", Options = JobContinuationOptions.OnAnyFinishedState},
                new Continuation {JobId = "3214324", Options = JobContinuationOptions.OnlyOnSucceededState}
            }, SerializationOption.User);

            var continuations = SerializationHelper.Deserialize<List<Continuation>>(continuationsJson);

            Assert.Equal(2, continuations.Count);
            Assert.Equal("1", continuations[0].JobId);
            Assert.Equal(JobContinuationOptions.OnAnyFinishedState, continuations[0].Options);
            Assert.Equal("3214324", continuations[1].JobId);
            Assert.Equal(JobContinuationOptions.OnlyOnSucceededState, continuations[1].Options);
        }
    }
}
