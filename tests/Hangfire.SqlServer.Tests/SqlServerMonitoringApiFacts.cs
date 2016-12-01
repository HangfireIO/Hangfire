using System;
using System.Runtime.Serialization.Formatters;
using Hangfire.Common;
using Hangfire.SqlServer.Entities;
using Newtonsoft.Json;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerMonitoringApiFacts
    {
        [Fact, CleanJsonSerializersSettings]
        public void HandlesChangingDeserializationMethodOfServerData()
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

            var serverData = new ServerData
            {
                WorkerCount = 5,
                Queues = new string[2] {"default", "critical"},
                StartedAt = new DateTime(2016, 12, 01, 14, 33, 00)
            };
            var serializedServerData = JobHelper.ToJson(serverData);

            var deserializedServerData = SerializationHelper.Deserialize<ServerData>(serializedServerData);

            Assert.Equal(5, deserializedServerData.WorkerCount);
            Assert.Equal(new string[2] { "default", "critical" }, deserializedServerData.Queues);
            Assert.Equal(new DateTime(2016, 12, 01, 14, 33, 00), deserializedServerData.StartedAt);
        }
    }
}