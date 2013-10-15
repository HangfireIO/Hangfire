using System.Collections.Generic;

namespace HangFire.Client
{
    public class CreateContext
    {
        internal CreateContext(CreateContext context)
            : this(context.JobDescriptor)
        {
            Items = context.Items;
        }

        internal CreateContext(ClientJobDescriptor jobDescriptor)
        {
            JobDescriptor = jobDescriptor;
            Items = new Dictionary<string, object>();
        }

        public IDictionary<string, object> Items { get; private set; }
        public ClientJobDescriptor JobDescriptor { get; private set; }
    }
}