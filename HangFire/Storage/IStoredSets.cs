namespace HangFire.Storage
{
    public interface IStoredSets
    {
        string GetFirstByLowestScore(string key, long fromScore, long toScore);
    }
}