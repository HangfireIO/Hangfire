namespace HangFire
{
    public abstract class HangFireJob
    {
        internal RedisClient Client { get; set; }

        public string JobId { get; internal set; }

        public abstract void Perform();

        public void Set(string propertyName, object value)
        {
            lock (Client)
            {
                Client.TryToDo(
                    x => x.SetJobProperty(JobId, propertyName, value),
                    throwOnError: true);
            }
        }

        public T Get<T>(string propertyName)
        {
            var value = default(T);

            lock (Client)
            {
                Client.TryToDo(
                    x => x.GetJobProperty<T>(JobId, propertyName),
                    throwOnError: true);
            }

            return value;
        }
    }
}
