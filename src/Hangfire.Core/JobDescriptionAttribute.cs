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
    /// Specifies an HTML description for a job method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class JobDescriptionAttribute : Attribute
    {
        private static readonly ConcurrentDictionary<Type, ResourceManager> _resourceManagerCache
            = new ConcurrentDictionary<Type, ResourceManager>();

        public JobDescriptionAttribute(string description)
        {
            if (string.IsNullOrEmpty(description))
                throw new ArgumentException("Description is empty", nameof(description));

            Description = description;
        }

        /// <summary>
        /// Gets description for the job.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets or sets resource type to localize <see cref="Description"/> string.
        /// </summary>
        public Type ResourceType { get; set; }

        public virtual string Format(DashboardContext context, Job job)
        {
            var format = Description;

            if (ResourceType != null)
            {
                format = _resourceManagerCache
                    .GetOrAdd(ResourceType, InitResourceManager)
                    .GetString(Description, CultureInfo.CurrentUICulture);

                if (string.IsNullOrEmpty(format))
                {
                    // failed to localize description string, use it as is
                    format = Description;
                }
            }
            
            return string.Format(CultureInfo.CurrentCulture, format, job.Args.ToArray());
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
