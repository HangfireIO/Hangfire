using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Hangfire
{
    /// <summary>
    /// Specifies a display name for a job method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class JobDisplayNameAttribute : Attribute
    {
        private static readonly ConcurrentDictionary<Type, ResourceManager> _resourceManagerCache
            = new ConcurrentDictionary<Type, ResourceManager>();

        public JobDisplayNameAttribute(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                throw new ArgumentException("Display name is empty", nameof(displayName));

            DisplayName = displayName;
        }

        /// <summary>
        /// Gets display name for the job.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets or sets resource type to localize <see cref="DisplayName"/> string.
        /// </summary>
        public Type ResourceType { get; set; }

        private static ResourceManager InitResourceManager(Type type)
        {
            var prop = type.GetTypeInfo().GetDeclaredProperty("ResourceManager");
            if (prop != null && prop.PropertyType == typeof(ResourceManager) && prop.CanRead && prop.GetMethod.IsStatic)
            {
                // use existing resource manager if possible
                return (ResourceManager) prop.GetValue(null);
            }
            
            return new ResourceManager(type);
        }
        
        internal string Format(params object[] arguments)
        {
            var format = DisplayName;

            if (ResourceType != null)
            {
                format = _resourceManagerCache
                    .GetOrAdd(ResourceType, InitResourceManager)
                    .GetString(DisplayName, CultureInfo.CurrentUICulture);

                if (string.IsNullOrEmpty(format))
                {
                    // failed to localize display name string, use it as is
                    format = DisplayName;
                }
            }
            
            return string.Format(CultureInfo.CurrentCulture, format, arguments);
        }
    }
}
