using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hangfire.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;
using static Hangfire.ContinuationsSupportAttribute;

namespace Hangfire.Core.Tests
{
    public class ContinuationsSupportAttributeFacts
    {
        [DataCompatibilityRangeFact, CleanSerializerSettings]
        public void HandlesChangingProcessOfInternalDataSerialization()
        {
            SerializationHelper.SetUserSerializerSettings(SerializerSettingsHelper.DangerousSettings);

            var continuationsJson = SerializationHelper.Serialize(new List<Continuation>
            {
                new Continuation {JobId = "1", Options = JobContinuationOptions.OnAnyFinishedState},
                new Continuation {JobId = "3214324", Options = JobContinuationOptions.OnlyOnSucceededState}
            }, SerializationOption.User);

            var continuations = SerializationHelper.Deserialize<List<Continuation>>(continuationsJson);

            Assert.NotNull(continuations);
            Assert.Equal(2, continuations.Count);
            Assert.Equal("1", continuations[0].JobId);
            Assert.Equal(JobContinuationOptions.OnAnyFinishedState, continuations[0].Options);
            Assert.Equal("3214324", continuations[1].JobId);
            Assert.Equal(JobContinuationOptions.OnlyOnSucceededState, continuations[1].Options);
        }

        // https://github.com/HangfireIO/Hangfire/issues/1470
        [DataCompatibilityRangeFact, CleanSerializerSettings]
        public void DeserializeContinuations_CanHandleFieldBasedSerialization_OfContinuationClass()
        {
#pragma warning disable 618
            JobHelper.SetSerializerSettings(new JsonSerializerSettings { ContractResolver = new FieldsOnlyContractResolver() });
#pragma warning restore 618
            var payload = "[{\"<JobId>k__BackingField\":\"123\",\"<Options>k__BackingField\":1},{\"<JobId>k__BackingField\":\"456\",\"<Options>k__BackingField\":0}]";

            var data = DeserializeContinuations(payload);

            Assert.Equal("123", data[0].JobId);
            Assert.Equal(JobContinuationOptions.OnlyOnSucceededState, data[0].Options);

            Assert.Equal("456", data[1].JobId);
            Assert.Equal(JobContinuationOptions.OnAnyFinishedState, data[1].Options);
        }

        private class FieldsOnlyContractResolver: DefaultContractResolver 
        {
            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
                => objectType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Cast<MemberInfo>()
                    .ToList();

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) 
                => base.CreateProperties(type, MemberSerialization.Fields);
        }
    }
}