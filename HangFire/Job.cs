using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HangFire
{
    public class Job
    {
        public Job(Type workerType, object args)
        {
            Jid = Guid.NewGuid().ToString();
            WorkerType = workerType;
            Args = new Dictionary<string, string>();
            EnqueuedAt = DateTime.UtcNow;

            AddValues(Args, args);
        }

        public string Jid { get; internal set; }
        public Type WorkerType { get; internal set; }
        public IDictionary<string, string> Args { get; internal set; }
        public DateTime EnqueuedAt { get; internal set; }

        [IgnoreDataMember]
        public bool Canceled { get; private set; }

        public void Cancel()
        {
            Canceled = true;
        }

        private static void AddValues(IDictionary<string, string> dictionary, object values)
        {
            if (values != null)
            {
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(values))
                {
                    var obj2 = descriptor.GetValue(values);
                    dictionary.Add(descriptor.Name, obj2 != null ? obj2.ToString() : null);
                }
            }
        }
    }
}