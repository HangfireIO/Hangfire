namespace HangFire.Storage
{
    public interface IWriteableStoredLists
    {
        void AddToLeft(string key, string value);
        void Remove(string key, string value);

        void Trim(string key, int keepStartingFrom, int keepEndingAt);
    }
}