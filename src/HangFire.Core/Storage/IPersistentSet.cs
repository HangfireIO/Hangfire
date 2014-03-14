namespace HangFire.Storage
{
    public interface IPersistentSet
    {
        string GetFirstByLowestScore(string key, long fromScore, long toScore);
    }
}