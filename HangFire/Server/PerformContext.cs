using System.Collections.Generic;

namespace HangFire.Server
{
    /// <summary>
    /// Provides information about the context in which the job
    /// is being performed.
    /// </summary>
    public class PerformContext : WorkerContext
    {
        internal PerformContext(PerformContext context)
            : this(context, context.JobDescriptor)
        {
            Items = context.Items;
        }

        internal PerformContext(
            WorkerContext workerContext, ServerJobDescriptor jobDescriptor)
            : base(workerContext)
        {
            JobDescriptor = jobDescriptor;
            Items = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets an instance of the key-value storage. You can use it
        /// to pass additional information between different client filters
        /// or just between different methods.
        /// </summary>
        public IDictionary<string, object> Items { get; private set; }

        /// <summary>
        /// Gets the client job descriptor that is associated with the
        /// current <see cref="PerformContext"/> object.
        /// </summary>
        public ServerJobDescriptor JobDescriptor { get; private set; }
    }
}
