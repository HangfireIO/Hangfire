using Hangfire.Annotations;

namespace Hangfire.Storage
{
    public interface IQueueWriteOnlyTransaction : IWriteOnlyTransaction
    {
        void AddToSetQueue([NotNull] string key, [NotNull] string value, [NotNull] string queueName);
        void AddToSetQueue([NotNull] string key, [NotNull] string value, [NotNull] string queueName, double score);
    }
}