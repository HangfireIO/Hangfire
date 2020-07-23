using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using Hangfire.Common;
using Hangfire.Dashboard;

namespace Hangfire
{
    /// <summary>
    /// A base class that represents attributes that provide additional string metadata about a job, such as a name
    /// or a description.
    /// </summary>
    public abstract class JobMetadataAttribute : Attribute
    {
        private static readonly ConcurrentDictionary<Type, ResourceManager> _resourceManagerCache
            = new ConcurrentDictionary<Type, ResourceManager>();

        public JobMetadataAttribute(string metadataValue)
        {
            if (string.IsNullOrEmpty(metadataValue))
                throw new ArgumentException("Metadata value is empty", nameof(metadataValue));

            MetadataValue = metadataValue;
        }

        /// <summary>
        /// Gets the metadata for this attribute.
        /// </summary>
        protected string MetadataValue { get; }

        /// <summary>
        /// Gets or sets resource type to localize the metadata string.
        /// </summary>
        public Type ResourceType { get; set; }

        public virtual string Format(DashboardContext context, Job job)
        {
            var formatted = MetadataValue;

            if (ResourceType != null)
            {
                formatted = _resourceManagerCache
                    .GetOrAdd(ResourceType, InitResourceManager)
                    .GetString(MetadataValue, CultureInfo.CurrentUICulture);

                if (string.IsNullOrEmpty(formatted))
                {
                    // failed to localize description string, use it as is
                    formatted = MetadataValue;
                }
            }
            
            return string.Format(CultureInfo.CurrentCulture, formatted, job.Args.ToArray());
        }

        private static ResourceManager InitResourceManager(Type type)
        {
            var prop = type.GetTypeInfo().GetDeclaredProperty("ResourceManager");
            if (prop != null && prop.PropertyType == typeof(ResourceManager) && prop.CanRead && prop.GetMethod.IsStatic)
            {
                // use existing resource manager if possible
                return (ResourceManager)prop.GetValue(null);
            }

            return new ResourceManager(type);
        }
    }
}