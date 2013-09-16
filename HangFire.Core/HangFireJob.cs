using System;
using System.ComponentModel;

namespace HangFire
{
    public abstract class HangFireJob
    {
        public WorkerContext WorkerContext { get; private set; }

        public abstract void Perform();

        public void SetParameter(string propertyName, object value)
        {
            lock (Worker.Redis)
            {
                Worker.Redis.SetJobProperty(WorkerContext.JobId, propertyName, value);
            }
        }

        public T GetParameter<T>(string propertyName)
        {
            lock (Worker.Redis)
            {
                return Worker.Redis.GetJobProperty<T>(WorkerContext.JobId, propertyName);
            }
        }

        internal void Initialize(WorkerContext workerContext)
        {
            if (workerContext == null) throw new ArgumentNullException("workerContext");

            WorkerContext = workerContext;

            foreach (var arg in WorkerContext.JobProperties)
            {
                var propertyInfo = GetType().GetProperty(arg.Key);
                if (propertyInfo != null)
                {
                    var converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);

                    // TODO: handle deserialization exception and display it in a friendly way.
                    var value = converter.ConvertFromInvariantString(arg.Value);
                    propertyInfo.SetValue(this, value, null);
                }
            }
        }
    }
}
