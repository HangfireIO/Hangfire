using System.Collections.Generic;
using Hangfire.Annotations;

namespace Hangfire.Storage
{
    public interface IQueueStorageConnection : IStorageConnection
    {
        HashSet<string> GetAllItemsFromSetQueue([NotNull] string key, [NotNull] string queueName);
        Dictionary<string, double> GetAllValuesWithScoresFromSetQueueWithinScoreRange(string key, string queueName, double fromScore, double toScore);
    }
}