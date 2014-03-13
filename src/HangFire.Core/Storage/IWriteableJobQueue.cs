namespace HangFire.Storage
{
    public interface IWriteableJobQueue
    {
        void Enqueue(string queue, string jobId);
    }
}