using System;
using Hangfire.Common;
using Hangfire.SqlServer.Entities;
using Xunit;

namespace Hangfire.SqlServer.Tests
{
    public class SqlServerMonitoringApiFacts
    {
        [Fact, CleanJsonSerializersSettings]
        public void HandlesChangingProcessOfServerDataSerialization()
        {
            SerializationHelper.SetUserSerializerSettings(SerializerSettingsHelper.DangerousSettings);

            var serverData = new ServerData
            {
                WorkerCount = 5,
                Queues = new[] {"default", "critical"},
                StartedAt = new DateTime(2016, 12, 01, 14, 33, 00)
            };
            var serializedServerData = SerializationHelper.Serialize(serverData, SerializationOption.User);

            var deserializedServerData = SerializationHelper.Deserialize<ServerData>(serializedServerData);

            Assert.Equal(5, deserializedServerData.WorkerCount);
            Assert.Equal(new[] { "default", "critical" }, deserializedServerData.Queues);
            Assert.Equal(new DateTime(2016, 12, 01, 14, 33, 00), deserializedServerData.StartedAt);
        }
    }
}