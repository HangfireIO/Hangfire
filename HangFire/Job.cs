using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HangFire
{
    internal class Job
    {
        public Job(Type workerType, object args)
        {
            Jid = Guid.NewGuid().ToString(); // TODO: convert it to 21 symb. string
            WorkerType = workerType;
            Args = new Dictionary<string, string>();
            EnqueuedAt = DateTime.UtcNow;

            AddValues(Args, args);
        }

        public string Jid { get; set; }
        public Type WorkerType { get; set; }
        public IDictionary<string, string> Args { get; set; }
        public DateTime EnqueuedAt { get; set; }

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