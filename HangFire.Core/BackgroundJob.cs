namespace HangFire
{
    public abstract class BackgroundJob
    {
        public string JobId { get; internal set; }

        public abstract void Perform();

        public void SetParameter(string propertyName, object value)
        {
            lock (Worker.Redis)
            {
                Worker.Redis.SetJobProperty(JobId, propertyName, value);
            }
        }

        public T GetParameter<T>(string propertyName)
        {
            lock (Worker.Redis)
            {
                return Worker.Redis.GetJobProperty<T>(JobId, propertyName);
            }
        }
    }
}
