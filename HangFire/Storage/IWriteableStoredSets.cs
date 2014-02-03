namespace HangFire.Storage
{
    public interface IWriteableStoredSets
    {
        void Add(string key, string value);
        void Add(string key, string value, double score);
        void Remove(string key, string value);
    }
}