using System;

namespace Hangfire
{
    /// <summary>
    /// Specifies an HTML description for a job method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class JobDescriptionAttribute : JobMetadataAttribute
    {
        public JobDescriptionAttribute(string description) : base(description)
        {
        }

        /// <summary>
        /// Gets description for the job.
        /// </summary>
        public string Description => MetadataValue;
    }
}
