using HangFire.Server;

namespace HangFire
{
    public abstract class BackgroundJob
    {
        public string JobId { get; internal set; }

        public abstract void Perform();

        public void SetParameter(string propertyName, object value)
        {
            lock (ThreadedWorker.Redis)
            {
                ThreadedWorker.Redis.SetJobProperty(JobId, propertyName, value);
            }
        }

        public T GetParameter<T>(string propertyName)
        {
            lock (ThreadedWorker.Redis)
            {
                return ThreadedWorker.Redis.GetJobProperty<T>(JobId, propertyName);
            }
        }
    }
}
