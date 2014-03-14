namespace HangFire.Storage
{
    public interface IWriteOnlyPersistentQueue
    {
        void Enqueue(string queue, string jobId);
    }
}