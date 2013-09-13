namespace HangFire
{
    public abstract class HangFireJob
    {
        internal RedisStorage Redis { get; set; }

        public string JobId { get; internal set; }

        public abstract void Perform();

        public void Set(string propertyName, object value)
        {
            lock (Redis)
            {
                Redis.SetJobProperty(JobId, propertyName, value);
            }
        }

        public T Get<T>(string propertyName)
        {
            lock (Redis)
            {
                return Redis.GetJobProperty<T>(JobId, propertyName);
            }
        }
    }
}
