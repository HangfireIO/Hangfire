using System;
using HangFire.Server;

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
                Worker.Redis.SetEntryInHash(
                    String.Format("hangfire:job:{0}", JobId),
                    propertyName,
                    JobHelper.ToJson(value));
            }
        }

        public T GetParameter<T>(string propertyName)
        {
            lock (Worker.Redis)
            {
                var value = Worker.Redis.GetValueFromHash(
                    String.Format("hangfire:job:{0}", JobId),
                    propertyName);

                return JobHelper.FromJson<T>(value);
            }
        }
    }
}
