using System;

namespace Hangfire
{
    /// <summary>
    /// Specifies a display name for a job method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class JobDisplayNameAttribute : JobMetadataAttribute
    {
        public JobDisplayNameAttribute(string displayName) : base(displayName)
        {
        }

        /// <summary>
        /// Gets display name for the job.
        /// </summary>
        public string DisplayName => MetadataValue;
    }
}
