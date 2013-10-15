using System.Collections.Generic;
using HangFire.Server;

namespace HangFire.Filters
{
    public class PerformContext : WorkerContext
    {
        public PerformContext(PerformContext context)
            : this(context, context.JobDescriptor)
        {
            Items = context.Items;
        }

        public PerformContext(WorkerContext workerContext, ServerJobDescriptor jobDescriptor)
            : base(workerContext)
        {
            JobDescriptor = jobDescriptor;
            Items = new Dictionary<string, object>();
        }

        public IDictionary<string, object> Items { get; private set; }
        public ServerJobDescriptor JobDescriptor { get; private set; }
    }
}
